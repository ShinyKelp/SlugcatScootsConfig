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

    [BepInPlugin("ShinyKelp.SlugcatScootsConfig", "Slugcat Scoots Config", "1.1.2")]
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
            if (SlugcatScootsConfigOptions.doubleJumpOverTriple.Value && jumpCount == 0 && self.slideCounter >= 10 && self.slideCounter <= 20 && self.standing && self.bodyMode == Player.BodyModeIndex.Stand)
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
                    self.jumpBoost += SlugcatScootsConfigOptions.middleJumpBoost.Value;
                else if (jumpCount == 2)
                    self.jumpBoost += SlugcatScootsConfigOptions.flipJumpBoost.Value;
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
                player.bodyChunks[0].vel.y += SlugcatScootsConfigOptions.corridorBoostForce.Value;
                player.bodyChunks[1].vel.y += SlugcatScootsConfigOptions.corridorBoostForce.Value;
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
                player.bodyChunks[0].vel.y += SlugcatScootsConfigOptions.failedCorridorBoostForce.Value;
                player.bodyChunks[1].vel.y += SlugcatScootsConfigOptions.failedCorridorBoostForce.Value;
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
                player.bodyChunks[0].vel.x += SlugcatScootsConfigOptions.corridorBoostForce.Value * 
                (player.bodyChunks[0].pos.x > player.bodyChunks[1].pos.x ? 1f : -1f);
                player.bodyChunks[1].vel.x += SlugcatScootsConfigOptions.corridorBoostForce.Value *
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
                player.bodyChunks[0].vel.x += SlugcatScootsConfigOptions.failedCorridorBoostForce.Value *
                (player.bodyChunks[0].pos.x > player.bodyChunks[1].pos.x ? 1f : -1f);
                player.bodyChunks[1].vel.x += SlugcatScootsConfigOptions.failedCorridorBoostForce.Value *
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
            c.Emit(OpCodes.Pop);
            c.EmitDelegate <Func<int>>(() =>
            {
                return SlugcatScootsConfigOptions.postCorridorBoostStun.Value;
            });
        }

        private void Player_UpdateAnimation(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            //Slide initial animation duration
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>("rollCounter"),
                x => x.MatchLdcI4(6)
            );

            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<int>>(() =>
            {
                return SlugcatScootsConfigOptions.slideInitDuration.Value;
            }
            );

            //Slide initial animation pushback
            //(it is very close after the duration, so index is not reset for convenience)

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(9.1f)
            );
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return SlugcatScootsConfigOptions.slideInitPushback.Value;
            }
            );

            //Slide acceleration
            //Go to slide animation -> if slugpup section, then exit the if and modify the local variable
            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(7),
                x => x.MatchStloc(23),
                x => x.MatchLdcR4(9),
                x => x.MatchStloc(24),
                x => x.MatchLdarg(0)
            );

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 24);

            c.EmitDelegate<Func<Player, float, float>>((player, num) =>
            {
                if (player.isSlugpup || (player.isGourmand && player.gourmandExhausted))
                    return num;
                return SlugcatScootsConfigOptions.slideAcceleration.Value;
            });

            c.Emit(OpCodes.Stloc, 24);

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 23);

            c.EmitDelegate<Func<Player, float, float>>((player, num) =>
            {
                if (player.isSlugpup || (player.isGourmand && player.gourmandExhausted))
                    return num;
                return SlugcatScootsConfigOptions.extendedSlideAcceleration.Value;
            });

            c.Emit(OpCodes.Stloc, 23);

            //Slide duration (adjustment of the sin movement function)
            //(it's right after setting the acceleration values, index is not reset for convenience)

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(15f)
                );
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return SlugcatScootsConfigOptions.slideDuration.Value;
            });
            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(39f)
                );
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return SlugcatScootsConfigOptions.extendedSlideDuration.Value;
            });

            //Slide Pounce window
            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(6),
                x => x.MatchStloc(27),
                x => x.MatchLdcI4(20),
                x => x.MatchStloc(28)
            );
            c.Index++;

            c.EmitDelegate<Func<int>>(() =>
            {
                return SlugcatScootsConfigOptions.slidePounceWindow.Value;
            });
            c.Emit(OpCodes.Stloc, 27);
            c.EmitDelegate<Func<int>>(() =>
            {
                return SlugcatScootsConfigOptions.extendedSlidePounceWindow.Value;
            });
            c.Emit(OpCodes.Stloc, 28);

            //Slide duration (is in same IF statement of pounce window, index is not reset for convenience).
            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(15)
                );
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<int>>(() =>
            {
                return SlugcatScootsConfigOptions.slideDuration.Value;
            });
            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(39)
                );
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<int>>(() =>
            {
                return SlugcatScootsConfigOptions.extendedSlideDuration.Value;
            });

            //Slide post-stun (slightly after duration)
            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(40)
            );
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<int>>(() =>
            {
                return SlugcatScootsConfigOptions.postSlideStun.Value;
            });
            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(20)
            );
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<int>>(() =>
            {
                return SlugcatScootsConfigOptions.standingPostSlideStun.Value;
            });

            //Roll duration
            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>("rollCounter"),
                x => x.MatchLdcI4(15)
            );
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<int>>(() =>
            {
                return SlugcatScootsConfigOptions.rollDuration.Value;
            });

            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>("rollCounter"),
                x => x.MatchConvR4(),
                x => x.MatchLdcR4(30f)
            );
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return SlugcatScootsConfigOptions.rollDuration.Value*2;
            });

            c.GotoNext(MoveType.After,
               x => x.MatchLdarg(0),
               x => x.MatchLdfld<Player>("rollCounter"),
               x => x.MatchConvR4(),
               x => x.MatchLdcR4(60f)
           );
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return SlugcatScootsConfigOptions.rollDuration.Value * 4;
            });

            //Roll speed
            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(18)
            );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Player>>((player) =>
            {
                if (player.isSlugpup)
                    return;
                player.bodyChunks[0].vel.x += SlugcatScootsConfigOptions.rollSpeed.Value * (float)player.rollDirection;
                player.bodyChunks[1].vel.x += SlugcatScootsConfigOptions.rollSpeed.Value * (float)player.rollDirection;
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
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return SlugcatScootsConfigOptions.poleBoostForce.Value;
            });
            c.Index++;
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return SlugcatScootsConfigOptions.poleBoostRegression.Value * -1f;
            });

            //Post-pole boost stun duration
            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>("slowMovementStun"),
                x => x.MatchLdcI4(16)
            );
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<int>>(() =>
            {
                return SlugcatScootsConfigOptions.postPoleBoostStun.Value;
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
                float xValue = SlugcatScootsConfigOptions.wallPounceX.Value;
                float yValue = SlugcatScootsConfigOptions.wallPounceY.Value;
                float stunValue = SlugcatScootsConfigOptions.postWallPounceStun.Value;
                player.bodyChunks[0].vel = new Vector2((float)direction.x * -xValue, yValue);
                player.bodyChunks[1].vel = new Vector2((float)direction.x * -xValue, yValue);
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
                player.bodyChunks[0].vel.y *= SlugcatScootsConfigOptions.rollPounceMultiplierY.Value;
                player.bodyChunks[1].vel.y *= SlugcatScootsConfigOptions.rollPounceMultiplierY.Value;
                player.bodyChunks[0].vel.x *= SlugcatScootsConfigOptions.rollPounceMultiplierX.Value;
                player.bodyChunks[1].vel.x *= SlugcatScootsConfigOptions.rollPounceMultiplierX.Value;

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
                float xValue = SlugcatScootsConfigOptions.slidePounceX.Value;
                float yValue = SlugcatScootsConfigOptions.slidePounceY.Value;
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
                float xValue = SlugcatScootsConfigOptions.whiplashX.Value;
                float yValue = SlugcatScootsConfigOptions.whiplashY.Value;
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
                float jumpValue = SlugcatScootsConfigOptions.backflip.Value;
                float floatValue = SlugcatScootsConfigOptions.backflipFloat.Value;
                player.bodyChunks[0].vel.y = (jumpValue+2) * num;
                player.bodyChunks[1].vel.y = jumpValue * num;
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
