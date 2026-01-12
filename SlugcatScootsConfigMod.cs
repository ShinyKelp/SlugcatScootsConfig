using System;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using System.Reflection;
using BepInEx.Bootstrap;
using System.Collections.Generic;
using TripleJump;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SlugcatScootsConfig
{
    using Bindings = SlugcatScootsConfigOptions;

    [BepInPlugin("ShinyKelp.SlugcatScootsConfig", "Slugcat Scoots Config", "2.0.0")]
    public partial class SlugcatScootsConfigMod : BaseUnityPlugin
    {
        public static bool hasTripleJump = false;
        HashSet<Player> playersToSkipJump = null;
        System.Collections.IDictionary _jumpCountDict;
        private void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
            On.RainWorld.PostModsInit += RainWorld_PostModsInit;
        }
        bool isActive = false;

        private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            if (hasTripleJump)
            {
                if(_jumpCountDict is null)
                    GetJumpCountInstance();
            }
        }

        private bool IsInit;
        private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            UnityEngine.Debug.Log("Initializing Slugcat Scoots Config.");
            orig(self);
            try
            {
                if (IsInit) return;

                //Your hooks go here
                On.RainWorldGame.ShutDownProcess += RainWorldGameOnShutDownProcess;
                On.GameSession.ctor += GameSessionOnctor;
                On.Player.Jump += Player_Jump;
                On.Player.Update += Player_Update;
                IL.Player.UpdateAnimation += Player_UpdateAnimation;
                IL.Player.UpdateBodyMode += Player_UpdateBodyMode;
                IL.Player.Jump += Player_Jump;
                IL.Player.TerrainImpact += Player_TerrainImpact;

                hasTripleJump = false;

                foreach(ModManager.Mod mod in ModManager.ActiveMods)
                {
                    if(mod.name == "Triple Jump")
                    {
                        hasTripleJump = true;
                        GetJumpCountInstance();
                        playersToSkipJump = new HashSet<Player>();
                        break;
                    }
                }
                MachineConnector.SetRegisteredOI("ShinyKelp.SlugcatScootsConfig", SlugcatScootsConfigOptions.instance);

                IsInit = true;
                UnityEngine.Debug.Log("Slugcat Scoots Config Initialized successfully!");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            if (hasTripleJump)
                SetJumpCounts(orig, self);
            orig(self, eu);
        }

        private void SetJumpCounts(On.Player.orig_Update orig, Player self)
        {
            if (playersToSkipJump.Contains(self))
            {
                //Literally impossible to change jumpCount within Player.Jump call; must be done elsewhere.
                _jumpCountDict[self] = 2;
                playersToSkipJump.Remove(self);
            }
        }

        #region Triple Jump

        private void GetJumpCountInstance()
        {
            foreach (PluginInfo plugin in Chainloader.PluginInfos.Values)
            {
                if (plugin.Metadata.GUID == "triplejump")
                {
                    BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                    // Get the AttachedField<Player, int> instance from the plugin
                    object attachedFieldInstance = plugin.Instance.GetType().GetField("_jumpCount", flags).GetValue(plugin.Instance);

                    // Now get the '_dict' field from that AttachedField instance
                    FieldInfo dictField = attachedFieldInstance.GetType().GetField("_dict", flags);

                    // Get the dictionary object (assuming it's of a compatible type like Dictionary<Player, int>)
                    object dictObj = dictField.GetValue(attachedFieldInstance);
                    _jumpCountDict = dictObj as System.Collections.IDictionary;
                    break;
                }
            }
        }

        private void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            if (!hasTripleJump)
            {
                orig(self);
                return;
            }
            else CustomTripleJump(orig, self);
        }

        //Code reads same checks as Triple Jump, modifies player.jumpBoost
        private void CustomTripleJump(On.Player.orig_Jump orig, Player self)
        {
            int jumpCount = (int)_jumpCountDict[self];
            bool bypassTurnCheck = false;

            //If true, second jump will be flip
            if (Bindings.doubleJumpOverTriple.Value && jumpCount == 0 && self.slideCounter >= 10 && self.slideCounter <= 20 && self.standing && self.bodyMode == Player.BodyModeIndex.Stand)
                playersToSkipJump.Add(self);

            //Checks wether TripleJump's call was done before or after this.
            if (jumpCount >= 2 && self.slideCounter == 1 && self.standing && self.bodyMode == Player.BodyModeIndex.Stand)
                bypassTurnCheck = true;
            else if (jumpCount >= 2 && self.slideCounter >= 10 && self.slideCounter <= 20 && self.standing && self.bodyMode == Player.BodyModeIndex.Stand)
                bypassTurnCheck = true;

            orig(self);

            if (bypassTurnCheck || (self.slideCounter >= 10 && self.slideCounter <= 20 && self.standing && self.bodyMode == Player.BodyModeIndex.Stand))
            {
                //Overwrite jumpBoost value
                self.jumpBoost -= 1.5f * jumpCount;
                //Custom amount
                if (jumpCount == 1)
                {
                    if (Bindings.middleJumpBoost.Value < -10f)
                        self.jumpBoost += 1.5f;
                    else
                        self.jumpBoost += Bindings.middleJumpBoost.Value;
                }
                else if (jumpCount == 2)
                {
                    if (Bindings.flipJumpBoost.Value < -10f)
                        self.jumpBoost += 3f;
                    else
                        self.jumpBoost += Bindings.flipJumpBoost.Value;
                }
            }
            
            
           
        }

        #endregion

        private void Player_UpdateBodyMode(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            //Corridor boost force
            //Vertical success
            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(22),
                x => x.MatchStfld<Player>("verticalCorridorSlideCounter")
            );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Player>>((player) =>
            {
                if (Bindings.corridorBoostForce.Value < 0f)
                    return;
                player.bodyChunks[0].vel.y += Bindings.corridorBoostForce.Value;
                player.bodyChunks[1].vel.y += Bindings.corridorBoostForce.Value;
            });
            //Vertical failure
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(34),
                x => x.MatchStfld<Player>("verticalCorridorSlideCounter")
            );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Player>>((player) =>
            {
                if (Bindings.failedCorridorBoostForce.Value < 0f)
                    return;
                player.bodyChunks[0].vel.y += Bindings.failedCorridorBoostForce.Value;
                player.bodyChunks[1].vel.y += Bindings.failedCorridorBoostForce.Value;
            });
            //Horizontal success
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(25),
                x => x.MatchStfld<Player>("horizontalCorridorSlideCounter")
            );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Player>>((player) =>
            {
                if (Bindings.corridorBoostForce.Value < 0f)
                    return;
                player.bodyChunks[0].vel.x += Bindings.corridorBoostForce.Value * 
                (player.bodyChunks[0].pos.x > player.bodyChunks[1].pos.x ? 1f : -1f);
                player.bodyChunks[1].vel.x += Bindings.corridorBoostForce.Value *
                (player.bodyChunks[0].pos.x > player.bodyChunks[1].pos.x ? 1f : -1f);
            });
            //Horizontal failure
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(15),
                x => x.MatchStfld<Player>("horizontalCorridorSlideCounter")
            );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Player>>((player) =>
            {
                if (Bindings.failedCorridorBoostForce.Value < 0f)
                    return;
                player.bodyChunks[0].vel.x += Bindings.failedCorridorBoostForce.Value *
                (player.bodyChunks[0].pos.x > player.bodyChunks[1].pos.x ? 1f : -1f);
                player.bodyChunks[1].vel.x += Bindings.failedCorridorBoostForce.Value *
                (player.bodyChunks[0].pos.x > player.bodyChunks[1].pos.x ? 1f : -1f);
            });
            //Post-boost stun
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(15),
                x => x.MatchStfld<Player>("slowMovementStun"),
                x => x.MatchLdcR4(0f)
            );
            c.Index -= 2;
            c.EmitDelegate <Func<int, int>>((orig) =>
            {
                if (Bindings.postCorridorBoostStun.Value < 0f)
                    return orig;
                else
                    return Bindings.postCorridorBoostStun.Value;
            });
        }

        private void Player_UpdateAnimation(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int lvib = 20;   //Local variable index base. If new variables shift the index of previous variables, increase this accordingly.

            //Slide initial animation duration
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>("rollCounter"),
                x => x.MatchLdcI4(6)
            );

            c.EmitDelegate<Func<int, int>>((orig) =>
            {
                if (Bindings.slideInitDuration.Value == -1)
                    return orig;
                else
                    return Bindings.slideInitDuration.Value;
            }
            );
            //Slide initial animation pushback
            //(it is very close after the duration, so index is not reset for convenience)

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(9.1f)
            );
            c.EmitDelegate<Func<float, float>>((orig) =>
            {
                if (Bindings.slideInitPushback.Value < 0f)
                    return orig;
                else
                    return Bindings.slideInitPushback.Value;
            }
            );

            //Slide acceleration
            //Go to slide animation -> if slugpup section, then exit the if and modify the local variable
            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(7),
                x => x.MatchStloc(lvib+6),
                x => x.MatchLdcR4(9),
                x => x.MatchStloc(lvib+7),
                x => x.MatchLdarg(0)
            );

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, lvib+7);

            c.EmitDelegate<Func<Player, float, float>>((player, num) =>
            {
                if (player.isSlugpup || (player.isGourmand && player.gourmandExhausted))
                    return num;
                if (Bindings.slideAcceleration.Value < 0f)
                    return num;
                else
                    return Bindings.slideAcceleration.Value;
            });

            c.Emit(OpCodes.Stloc, lvib + 7);

            //Extended slide
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, lvib + 6);

            c.EmitDelegate<Func<Player, float, float>>((player, num) =>
            {
                if (player.isSlugpup || (player.isGourmand && player.gourmandExhausted))
                    return num;
                if (Bindings.extendedSlideAcceleration.Value < 0f)
                    return num;
                return Bindings.extendedSlideAcceleration.Value;
            });

            c.Emit(OpCodes.Stloc, lvib + 6);

            //Slide duration (adjustment of the sin movement function)
            //(it's right after setting the acceleration values, index is not reset for convenience)

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(15f)
                );
            c.EmitDelegate<Func<float, float>>((orig) =>
            {
                if (Bindings.slideDuration.Value < 0f)
                    return orig;
                else
                    return Bindings.slideDuration.Value;
            });
            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(39f)
                );
            c.EmitDelegate<Func<float, float>>((orig) =>
            {
                if (Bindings.extendedSlideDuration.Value < 0f)
                    return orig;
                else
                    return Bindings.extendedSlideDuration.Value;
            });

            //Slide Pounce window
            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(6),
                x => x.MatchStloc(lvib + 12),
                x => x.MatchLdcI4(20),
                x => x.MatchStloc(lvib + 13)
            );
            c.Index++;
            c.Emit(OpCodes.Ldloc, lvib + 12);
            c.EmitDelegate<Func<int,int>>((orig) =>
            {
                if(Bindings.slidePounceWindow.Value < 0f) 
                    return orig;
                else
                    return Bindings.slidePounceWindow.Value;
            });
            c.Emit(OpCodes.Stloc, lvib + 12);

            c.Emit(OpCodes.Ldloc, lvib + 13);
            c.EmitDelegate<Func<int, int>>((orig) =>
            {
                if (Bindings.extendedSlidePounceWindow.Value < 0f)
                    return orig;
                else
                    return Bindings.extendedSlidePounceWindow.Value;
            });
            c.Emit(OpCodes.Stloc, lvib + 13);

            //Slide duration (is in same IF statement of pounce window, index is not reset for convenience).
            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(15)
                );
            c.EmitDelegate<Func<int, int>>((orig) =>
            {
                if (Bindings.slideDuration.Value < 0f)
                    return orig;
                else
                    return Bindings.slideDuration.Value;
            });
            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(39)
                );
            c.EmitDelegate<Func<int, int>>((orig) =>
            {
                if (Bindings.extendedSlideDuration.Value < 0f)
                    return orig;
                else
                    return Bindings.extendedSlideDuration.Value;
            });

            //Slide post-stun (slightly after duration)
            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(40)
            );
            c.EmitDelegate<Func<int, int>>((orig) =>
            {
                if (Bindings.postSlideStun.Value < 0f)
                    return orig;
                else
                    return Bindings.postSlideStun.Value;
            });
            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(20)
            );
            c.EmitDelegate<Func<int, int>>((orig) =>
            {
                if (Bindings.standingPostSlideStun.Value < 0f)
                    return orig;
                else
                    return Bindings.standingPostSlideStun.Value;
            });

            //Roll duration
            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>("rollCounter"),
                x => x.MatchLdcI4(15)
            );
            c.EmitDelegate<Func<int, int>>((orig) =>
            {
                if (Bindings.rollDuration.Value < 0f)
                    return orig;
                else
                    return Bindings.rollDuration.Value;
            });

            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>("rollCounter"),
                x => x.MatchConvR4(),
                x => x.MatchLdcR4(30f)
            );
            c.EmitDelegate<Func<float, float>>((orig) =>
            {
                if (Bindings.rollDuration.Value < 0f)
                    return orig;
                else
                    return Bindings.rollDuration.Value*2;
            });

            c.GotoNext(MoveType.After,
               x => x.MatchLdarg(0),
               x => x.MatchLdfld<Player>("rollCounter"),
               x => x.MatchConvR4(),
               x => x.MatchLdcR4(60f)
           );
            c.EmitDelegate<Func<float, float>>((orig) =>
            {
                if (Bindings.rollDuration.Value < 0f)
                    return orig;
                else
                    return Bindings.rollDuration.Value * 4;
            });

            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(lvib + 1)
            );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Player>>((player) =>
            {
                if (player.isSlugpup)
                    return;
                if (Bindings.rollSpeed.Value < 0f)
                    return;
                player.bodyChunks[0].vel.x += Bindings.rollSpeed.Value * (float)player.rollDirection;
                player.bodyChunks[1].vel.x += Bindings.rollSpeed.Value * (float)player.rollDirection;
            });

            //Pole boost and regression
            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchConvR4(),
                x => x.MatchLdcR4(17f),
                x => x.MatchLdcR4(0f),
                x => x.MatchLdcR4(3f),
                x => x.MatchLdcR4(-1.2f),
                x => x.MatchLdcR4(0.45f)
            );
            c.Index -= 2;
            c.EmitDelegate<Func<float, float>>((orig) =>
            {
                if (Bindings.poleBoostForce.Value < 0f)
                    return orig;
                else
                    return Bindings.poleBoostForce.Value;
            });
            c.Index++;
            c.EmitDelegate<Func<float, float>>((orig) =>
            {
                if (Bindings.poleBoostRegression.Value < 0f)
                    return orig;
                else
                    return Bindings.poleBoostRegression.Value * -1f;
            });

            //Post-pole boost stun duration
            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>("slowMovementStun"),
                x => x.MatchLdcI4(16)
            );
            c.EmitDelegate<Func<int, int>>((orig) =>
            {
                if (Bindings.postPoleBoostStun.Value < 0f)
                    return orig;
                else
                    return Bindings.postPoleBoostStun.Value;
            });

        }

        private void Player_TerrainImpact(ILContext il)
        {

            ILCursor c = new ILCursor(il);
            //Wallpounce
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld<MoreSlugcats.MMF>("cfgWallpounce")
            );

            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdsfld<Player.AnimationIndex>("RocketJump"),
                x => x.MatchStfld<Player>("animation")
            );

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_2);
            c.EmitDelegate<Action<Player, IntVector2>>((player, direction) =>
            {
                if (player.isSlugpup)
                    return;
                float xValue = Bindings.wallPounceX.Value;
                float yValue = Bindings.wallPounceY.Value;
                float stunValue = Bindings.postWallPounceStun.Value;
                if(xValue >= 0f && yValue >= 0f)
                {
                    player.bodyChunks[0].vel = new Vector2((float)direction.x * -xValue, yValue);
                    player.bodyChunks[1].vel = new Vector2((float)direction.x * -xValue, yValue);
                }
                if(stunValue >= 0f)
                    player.jumpStun = (int)stunValue * -direction.x;
            });
        }

        private void Player_Jump(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            //Roll pounce config

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(1.5f),
                x => x.MatchMul(),
                x => x.MatchStindR4(),
                x => x.MatchLdarg(0),
                x => x.MatchLdsfld<Player.AnimationIndex>("RocketJump"),
                x => x.MatchStfld<Player>("animation")
            );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Player>>((player) =>
            {
                if (player.isSlugpup)
                    return;
                if(Bindings.rollPounceMultiplierY.Value >= 0f)
                {
                    player.bodyChunks[0].vel.y *= Bindings.rollPounceMultiplierY.Value;
                    player.bodyChunks[1].vel.y *= Bindings.rollPounceMultiplierY.Value;
                }
                if(Bindings.rollPounceMultiplierX.Value >= 0f)
                {
                    player.bodyChunks[0].vel.x *= Bindings.rollPounceMultiplierX.Value;
                    player.bodyChunks[1].vel.x *= Bindings.rollPounceMultiplierX.Value;
                }
            });

            //Belly slide config
            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdsfld<Player.AnimationIndex>("RocketJump"),
                x => x.MatchStfld<Player>("animation"),
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(1),
                x => x.MatchStfld<Player>("rocketJumpFromBellySlide")
            );

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate<Action<Player, float>>((player, num) =>
            {
                if (player.isSlugpup)
                    return;
                float xValue = Bindings.slidePounceX.Value;
                float yValue = Bindings.slidePounceY.Value;
                if (xValue < 0f || yValue < 0f)
                    return;
                player.bodyChunks[1].vel = new Vector2((float)player.rollDirection * xValue, yValue) * num * (player.longBellySlide ? 1.2f : 1f);
                player.bodyChunks[0].vel = new Vector2((float)player.rollDirection * xValue, yValue) * num * (player.longBellySlide ? 1.2f : 1f);
            });

            //Whiplash pounce
            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(1),
                x => x.MatchStfld<Player>("flipFromSlide"),
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(0),
                x => x.MatchStfld<Player>("whiplashJump"),
                x => x.MatchLdarg(0),
                x => x.MatchLdcR4(0f),
                x => x.MatchStfld<Player>("jumpBoost")
            );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Player>>((player) =>
            {
                if (player.isSlugpup)
                    return;
                float xValue = Bindings.whiplashX.Value;
                float yValue = Bindings.whiplashY.Value;
                if (xValue < 0f || yValue < 0f)
                    return;
                player.bodyChunks[0].vel = new Vector2((float)player.rollDirection * xValue, yValue);
                player.bodyChunks[1].vel = new Vector2((float)player.rollDirection * xValue, yValue+1);
            });

            //Backflip
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcR4(3f),
                x => x.MatchStfld<Player>("jumpBoost"),
                x => x.MatchLdarg(0),
                x => x.MatchLdsfld<Player.AnimationIndex>("Flip"),
                x => x.MatchStfld<Player>("animation")
            );


            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate<Action<Player, float>>((player, num) =>
            {
                if (player.isSlugpup || player.PainJumps)
                    return;
                float jumpValue = Bindings.backflip.Value;
                float floatValue = Bindings.backflipFloat.Value;
                if(jumpValue >= 0f)
                {
                    player.bodyChunks[0].vel.y = (jumpValue + 2) * num;
                    player.bodyChunks[1].vel.y = jumpValue * num;
                }
                if(floatValue >= 0f)
                    player.jumpBoost = floatValue;
            });
            //*/
        }

        private GameSession game;

        private void RainWorldGameOnShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            orig(self);
            ClearMemory();
        }
        private void GameSessionOnctor(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
        {
            orig(self, game);
            ClearMemory();
            this.game = self;
        }

        #region Helper Methods

        private void ClearMemory()
        {
            //If you have any collections (lists, dictionaries, etc.)
            //Clear them here to prevent a memory leak
            //YourList.Clear();
        }

        #endregion
    }
}
