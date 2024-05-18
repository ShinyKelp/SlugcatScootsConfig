using System;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using UnityEngine;
using Random = UnityEngine.Random;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ScavengerTweaks
{

    [BepInPlugin("ShinyKelp.ScavengerTweaks", "Scavenger Tweaks", "1.0.0")]
    public partial class ScavengerTweaksMod : BaseUnityPlugin
    {
        RainWorldGame game;
        private void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
        }

        public ScavGearLevels SelectedLevel
        {
            get
            {
                if (options is null)
                    return 0;
                return (ScavGearLevels)(options.ScavengerGearLevel);
            }
        }

        public enum ScavGearLevels
        {
            VANILLA, REDUCED_EXPLOSIVES, EMPTY
        }

        private ScavengerTweaksOptions options;
        private bool IsConstructingScav = false;
        private bool IsInit;
        private bool hasStrongScavs = false;
        private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (IsInit) return;

                hasStrongScavs = false;
                foreach (ModManager.Mod mod in ModManager.ActiveMods)
                    if (mod.name == "Strong Scavs")
                    {
                        hasStrongScavs = true;
                        break;
                    }

                options = new ScavengerTweaksOptions(hasStrongScavs);
                
                //Your hooks go here
                On.ScavengerAI.CollectScore_PhysicalObject_bool += CollectBugSpearAndFireEgg;
                On.ScavengerAI.WeaponScore += ThrowBugSpearAndFireEgg;
                On.ScavengerAI.RealWeapon += ScavTakeFireEggs;
                On.RainWorldGame.ctor += RainWorldGame_ctor;
                On.AbstractCreature.ctor += StrongKing;
                On.AbstractCreature.Personality.ctor += EnhanceScavPersonalities;

                On.ScavengerAI.Update += TrickKingIntoSquad;
                On.ScavengerAI.DecideBehavior += TrickKingIntoSquadBehaviour;
                On.ScavengerAbstractAI.AbstractBehavior += ScavKingAbsAICanGoIntoPipes;
                On.StaticWorld.InitStaticWorld += ScavKingCanTravelLikeElites;
                IL.Scavenger.Act += ScavKingActCanGoIntoPipes;
                On.Scavenger.ctor += KingNotWaiting;
                On.Scavenger.SetUpCombatSkills += ModifyScavengerCombatSkills;
                IL.Scavenger.FlyingWeapon += ScavSensesFlyingWeapon;
                On.ScavengerAbstractAI.InitGearUp += ModifyScavengerGear;
                On.ScavengerAbstractAI.IsSpearExplosive += ModifyExplosiveSpearChance;
                IL.AbstractCreature.ctor += StrongScavReplacementChance;//*/
                On.Scavenger.Violence += Scavenger_Violence;

                Debug.Log("Set all hooks for ScavengerTweaks!");

                MachineConnector.SetRegisteredOI("ShinyKelp.ScavengerTweaks", options);

                Debug.Log("Set ScavengerTweaks options interface!");
                IsInit = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        private void Scavenger_Violence(On.Scavenger.orig_Violence orig, Scavenger self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            float healthMultiplier = options.healthMultiplier.Value;
            float finalMultiplier = 1f;
            if (options.variableHealth.Value)
            {
                finalMultiplier /= (self.abstractCreature.personality.bravery / 4f + 1);
                finalMultiplier /= (self.abstractCreature.personality.dominance / 2f + 1);
                finalMultiplier *= (self.abstractCreature.personality.nervous / 6f + 1);
            }
            finalMultiplier /= healthMultiplier;
            damage *= finalMultiplier;
            stunBonus *= (finalMultiplier + 1) / 2f;
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private void LogID(On.Scavenger.orig_Update orig, Scavenger self, bool eu)
        {
            UnityEngine.Debug.Log(self.abstractCreature.ID);
            orig(self, eu);
        }

        private void ModifyScavengerGear(On.ScavengerAbstractAI.orig_InitGearUp orig, ScavengerAbstractAI self)
        {
            if (SelectedLevel != ScavGearLevels.VANILLA && (self.world.game.IsArenaSession || self.parent.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing))
            {
                if (self.parent.creatureTemplate.type == CreatureTemplate.Type.Scavenger)
                    Scav_InitGearUp(self);
                else if (self.parent.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite)
                    EliteScav_InitGearUp(self);
                else if (self.parent.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing)
                    KingScav_InitGearUp(self);
            }
            else
                orig(self);
        }

        private void Scav_InitGearUp(ScavengerAbstractAI self) 
        {
            if (SelectedLevel == ScavGearLevels.EMPTY)
                return;

            UnityEngine.Random.InitState(self.parent.ID.RandomSeed);
            int spacesLeft = 4;
            int givenSpears = RWCustom.Custom.IntClamp((int)(Mathf.Pow(Random.value, Mathf.Lerp(1.5f, 0.5f, Mathf.Pow(self.parent.personality.dominance, 2f))) * (4.5f)), 0, 4);
            float dominance = self.parent.personality.dominance;

            AbstractPhysicalObject abstractPhysicalObject;

            for (int i = 0; i < givenSpears; ++i)
            {
                abstractPhysicalObject = new AbstractSpear(self.world, null, self.parent.pos, self.world.game.GetNewID(), self.IsSpearExplosive(40));
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject, spacesLeft - 1, true);
                spacesLeft--;
            }

            //Bombs
            if (spacesLeft > 0 && Random.value < (SelectedLevel == ScavGearLevels.REDUCED_EXPLOSIVES ? 0.2f : 0.6f) * dominance)
            {
                
                AbstractPhysicalObject abstractPhysicalObject3;
                abstractPhysicalObject3 = new AbstractPhysicalObject(self.world, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null, self.parent.pos, self.world.game.GetNewID());
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject3);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject3, spacesLeft - 1, true);
                spacesLeft--;
                
            }

            //Firecracker
            if (spacesLeft > 0 && Random.value < 0.32f * dominance)
            {
                AbstractPhysicalObject abstractPhysicalObject4 = new AbstractConsumable(self.world, AbstractPhysicalObject.AbstractObjectType.FirecrackerPlant,
                    null, self.parent.pos, self.world.game.GetNewID(), -1, -1, null);
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject4);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject4, spacesLeft - 1, true);
                spacesLeft--;
            }

            //Beecone
            if (spacesLeft > 0 && Random.value < 0.18f * dominance)
            {
                SporePlant.AbstractSporePlant abstractSporePlant = new SporePlant.AbstractSporePlant(self.world, null, self.parent.pos, self.world.game.GetNewID(), -1, -1, null, false, true);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractSporePlant, spacesLeft - 1, true);
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractSporePlant);
                spacesLeft--;
            }

            //Gooieduck
            if(spacesLeft > 0 && Random.value < 0.04f * dominance)
            {
                AbstractConsumable abstractConsumable2 = new AbstractConsumable(self.world, MoreSlugcatsEnums.AbstractObjectType.GooieDuck, null, self.parent.pos, self.world.game.GetNewID(), -1, -1, null);
                abstractConsumable2.isConsumed = true;
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractConsumable2);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractConsumable2, spacesLeft-1, true);
            }
        }
        private void EliteScav_InitGearUp(ScavengerAbstractAI self)
        {
            if (SelectedLevel == ScavGearLevels.EMPTY)
                return;
            UnityEngine.Random.InitState(self.parent.ID.RandomSeed);
            int spacesLeft = 4;
            int givenSpears = RWCustom.Custom.IntClamp((int)(Mathf.Pow(Random.value, Mathf.Lerp(1.5f, 0.5f, Mathf.Pow(self.parent.personality.dominance, 2f))) * (4.5f)), 0, 4);
            float dominance = self.parent.personality.dominance;

            if (givenSpears == 0)
                givenSpears = 1;
            //Spears
            for (int i = 0; i < givenSpears; ++i)
            {
                AbstractPhysicalObject abstractPhysicalObject;
                if (Random.value < 0.6f)
                {
                    abstractPhysicalObject = new AbstractSpear(self.world, null, self.parent.pos, self.world.game.GetNewID(), self.IsSpearExplosive(40));
                }
                else
                {
                    abstractPhysicalObject = new AbstractSpear(self.world, null, self.parent.pos, self.world.game.GetNewID(), false, true);
                }
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject, spacesLeft - 1, true);
                spacesLeft--;

            }
            //Lantern
            if (spacesLeft > 0 && Random.value < (SelectedLevel == ScavGearLevels.REDUCED_EXPLOSIVES ? 0.0625f : 0.4f) * dominance)
            {
                AbstractPhysicalObject abstractPhysicalObject2 = new AbstractPhysicalObject(self.world, AbstractPhysicalObject.AbstractObjectType.Lantern, null, self.parent.pos, self.world.game.GetNewID());
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject2);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject2, spacesLeft - 1, true);
                spacesLeft--;
            }

            //Bombs
            if (spacesLeft > 0 && Random.value < (SelectedLevel == ScavGearLevels.REDUCED_EXPLOSIVES ? 0.2f : 0.6f) * dominance)
            {
                    AbstractPhysicalObject abstractPhysicalObject3;
                    abstractPhysicalObject3 = new AbstractPhysicalObject(self.world,
                        AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null, self.parent.pos, self.world.game.GetNewID());
                    self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject3);
                    new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject3, spacesLeft - 1, true);
                    spacesLeft--;
            }

            //Singularity bombs
            if (spacesLeft > 0 && Random.value < (SelectedLevel == ScavGearLevels.REDUCED_EXPLOSIVES ? 0.02f : 0.35f) * dominance)
            {
                AbstractPhysicalObject abstractPhysicalObject3;
                abstractPhysicalObject3 = new AbstractPhysicalObject(self.world,
                    AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null, self.parent.pos, self.world.game.GetNewID());
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject3);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject3, spacesLeft - 1, true);
                spacesLeft--;
            }

            //Firecracker
            if (spacesLeft > 0 && Random.value < 0.32f * dominance)
            {
                AbstractPhysicalObject abstractPhysicalObject4 = new AbstractConsumable(self.world, AbstractPhysicalObject.AbstractObjectType.FirecrackerPlant,
                    null, self.parent.pos, self.world.game.GetNewID(), -1, -1, null);
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject4);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject4, spacesLeft - 1, true);
                spacesLeft--;
            }

            //Beecone
            if (spacesLeft > 0 && Random.value < 0.07f * dominance)
            {
                SporePlant.AbstractSporePlant abstractSporePlant = new SporePlant.AbstractSporePlant(self.world, null, self.parent.pos, self.world.game.GetNewID(), -1, -1, null, false, true);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractSporePlant, spacesLeft-1, true);
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractSporePlant);
                spacesLeft--;
            }


        }
        private void KingScav_InitGearUp(ScavengerAbstractAI self)
        {
            if (SelectedLevel == ScavGearLevels.EMPTY)
                return;
            UnityEngine.Random.InitState(self.parent.ID.RandomSeed);
            int spacesLeft = 4;
            float dominance = self.parent.personality.dominance;

            //One glowweed, guaranteed for the video (will be chance later).
            AbstractConsumable abstractConsumable = new AbstractConsumable(self.world, MoreSlugcatsEnums.AbstractObjectType.GlowWeed, null, self.parent.pos, self.world.game.GetNewID(), -1, -1, null);
            self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractConsumable);
            new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractConsumable, spacesLeft - 1, true);
            spacesLeft--;

            int givenSpears = RWCustom.Custom.IntClamp((int)(Mathf.Pow(Random.value, Mathf.Lerp(1.5f, 0.5f, Mathf.Pow(self.parent.personality.dominance, 2f))) * (4.5f)), 0, 4);

            if (givenSpears == 0)
                givenSpears++;
            if (givenSpears == 1 && Random.value > 0.5f)
                givenSpears++;
            if (givenSpears > spacesLeft)
                givenSpears = spacesLeft;
            //Spears
            float hue = Mathf.Lerp(0.35f, 0.6f, RWCustom.Custom.ClampedRandomVariation(0.5f, 0.5f, 2f));
            for (int i = 0; i < givenSpears; ++i)
            {
                AbstractPhysicalObject abstractPhysicalObject;
                //Firebug spear (none for video)
                if(Random.value < -0.35f)
                {
                    abstractPhysicalObject = new AbstractSpear(self.world, null, self.parent.pos, self.world.game.GetNewID(), false, hue);
                }
                //Explosive spears
                else if (Random.value < 0.5f)
                {
                    abstractPhysicalObject = new AbstractSpear(self.world, null, self.parent.pos, self.world.game.GetNewID(), self.IsSpearExplosive(40));
                }
                //Electric spears (normal for video)
                else
                {
                    abstractPhysicalObject = new AbstractSpear(self.world, null, self.parent.pos, self.world.game.GetNewID(), false, false); //false, true for electric
                }
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject, spacesLeft - 1, true);
                spacesLeft--;

            }
            //GlowWeed (has one guaranteed for now)
            if (spacesLeft > 0 && Random.value < -0.0625f)
            {
                AbstractPhysicalObject abstractPhysicalObject2 = new AbstractPhysicalObject(self.world, AbstractPhysicalObject.AbstractObjectType.Lantern, null, self.parent.pos, self.world.game.GetNewID());
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject2);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject2, spacesLeft - 1, true);
                spacesLeft--;
            }

            //FireEggs
            if (spacesLeft > 0 && Random.value < ((SelectedLevel == ScavGearLevels.REDUCED_EXPLOSIVES && self.world.game.IsArenaSession) ? 0.9f : 0.6f) * dominance)
            {

                AbstractPhysicalObject abstractPhysicalObject3;
                hue = Mathf.Lerp(0.35f, 0.6f, RWCustom.Custom.ClampedRandomVariation(0.5f, 0.5f, 2f));
                abstractPhysicalObject3 = new FireEgg.AbstractBugEgg(self.world, null, self.parent.pos, self.world.game.GetNewID(), hue);
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject3);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject3, spacesLeft - 1, true);
                spacesLeft--;

                if(spacesLeft > 0 && Random.value < 0.5f * dominance)
                {
                    AbstractPhysicalObject abstractPhysicalObject4;
                    hue = Mathf.Lerp(0.35f, 0.6f, RWCustom.Custom.ClampedRandomVariation(0.5f, 0.5f, 2f));
                    abstractPhysicalObject4 = new FireEgg.AbstractBugEgg(self.world, null, self.parent.pos, self.world.game.GetNewID(), hue);
                    self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject4);
                    new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject4, spacesLeft - 1, true);
                    spacesLeft--;
                }
                
            }

            //Bombs
            if (spacesLeft > 0 && Random.value < ((SelectedLevel == ScavGearLevels.REDUCED_EXPLOSIVES && self.world.game.IsArenaSession) ? 0.2f : 0.5f) * dominance)
            {
                AbstractPhysicalObject abstractPhysicalObject3;
                //Bomb type
                abstractPhysicalObject3 = new AbstractPhysicalObject(self.world, Random.value > 0.8f ?
                    AbstractPhysicalObject.AbstractObjectType.ScavengerBomb : AbstractPhysicalObject.AbstractObjectType.Rock, null, self.parent.pos, self.world.game.GetNewID());
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject3);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject3, spacesLeft - 1, true);
                spacesLeft--;
            }
            Debug.Log("SPACES LEFT: " + spacesLeft);
        }

        private bool ModifyExplosiveSpearChance(On.ScavengerAbstractAI.orig_IsSpearExplosive orig, ScavengerAbstractAI self, int cycleNum)
        {
            if (self.world.game.IsArenaSession && SelectedLevel == ScavGearLevels.REDUCED_EXPLOSIVES && Random.value > (self.parent.creatureTemplate.type == CreatureTemplate.Type.Scavenger ? 0.7f : 0.85f))
                return false;
            else return orig(self, cycleNum);
        }

        private void EnhanceScavPersonalities(On.AbstractCreature.Personality.orig_ctor orig, ref AbstractCreature.Personality self, EntityID ID)
        {
            orig(ref self, ID);
            if (IsConstructingScav)
            {
                
                self.aggression += options.aggression.Value;
                self.bravery += options.bravery.Value;
                self.dominance += options.dominance.Value;
                self.energy += options.energy.Value;
                self.nervous += options.nervous.Value;
                self.sympathy += options.sympathy.Value;

                if (self.aggression < 0.01f)
                    self.aggression = 0.01f;
                if (self.bravery < 0.01f)
                    self.bravery = 0.01f;
                if (self.dominance < 0.01f)
                    self.dominance = 0.01f;
                if (self.energy < 0.01f)
                    self.energy = 0.01f;
                if (self.nervous < 0.01f)
                    self.nervous = 0.01f;
                if (self.sympathy < 0.01f)
                    self.sympathy = 0.01f;

                //*/
                /*
                self.aggression += 0.2f;
                self.bravery += 2f;
                self.energy += 0.3f;
                self.nervous += 0.4f;
                //*/
            }
        }

        private void ModifyScavengerCombatSkills(On.Scavenger.orig_SetUpCombatSkills orig, Scavenger self)
        {
            orig(self);
            
            self.dodgeSkill += options.dodgeSkill.Value;
            self.meleeSkill += options.meleeSkill.Value;
            self.midRangeSkill += options.midRangeSkill.Value;
            self.blockingSkill += options.blockingSkill.Value;
            self.reactionSkill += options.reactionSkill.Value;

            if (self.dodgeSkill < 0.01f)
                self.dodgeSkill = 0.01f;
            if (self.meleeSkill < 0.01f)
                self.meleeSkill = 0.01f;
            if (self.midRangeSkill < 0.01f)
                self.midRangeSkill = 0.01f;
            if (self.blockingSkill < 0.01f)
                self.blockingSkill = 0.01f;
            if (self.reactionSkill < 0.01f)
                self.reactionSkill = 0.01f;

            //*/

            /*
            self.dodgeSkill += 0.4f;
            self.meleeSkill += 10f;
            self.midRangeSkill += 0.7f;
            self.blockingSkill += 4f;
            self.reactionSkill += 1000f;
            //*/
        }

        private void StrongKing(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
        {

            if (world is null || world.game is null)
            {
                Debug.Log("DETECTED NULL CREATURE ATTEMPT. PREVENTING CRASH.");
                self.creatureTemplate = creatureTemplate;
                self.personality = new AbstractCreature.Personality(ID);
                self.remainInDenCounter = -1;
                return;
            }
            if (creatureTemplate != null && creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Scavenger)
                IsConstructingScav = true;
            else IsConstructingScav = false;
            if (creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing)
            {
                AbstractCreature abs = new AbstractCreature(world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite), realizedCreature, pos, ID);
                orig(self, world, creatureTemplate, realizedCreature, pos, abs.ID);
            }
            else orig(self, world, creatureTemplate, realizedCreature, pos, ID);
        }

        #region FireEggs & Bugspears

        private bool ScavTakeFireEggs(On.ScavengerAI.orig_RealWeapon orig, ScavengerAI self, PhysicalObject obj)
        {
            return (orig(self, obj) || obj is FireEgg);
        }

        private int ThrowBugSpearAndFireEgg(On.ScavengerAI.orig_WeaponScore orig, ScavengerAI self, PhysicalObject obj, bool pickupDropInsteadOfWeaponSelection)
        {
            if (obj is FireEgg egg )
            {
                if (pickupDropInsteadOfWeaponSelection)
                {
                    if (egg.activeCounter > 0)
                        return 0;
                    else
                        return 4;
                }
                if (self.currentViolenceType != ScavengerAI.ViolenceType.Lethal)
                {
                    return 0;
                }
                if (self.focusCreature != null && !RWCustom.Custom.DistLess(self.scavenger.mainBodyChunk.pos, self.scavenger.room.MiddleOfTile(self.focusCreature.BestGuessForPosition()), 300f))
                {
                    for (int j = 0; j < self.tracker.CreaturesCount; j++)
                    {
                        if (self.tracker.GetRep(j).dynamicRelationship.currentRelationship.type == CreatureTemplate.Relationship.Type.Pack && (float)RWCustom.Custom.ManhattanDistance(self.tracker.GetRep(j).BestGuessForPosition(), self.focusCreature.BestGuessForPosition()) < 7f)
                        {
                            return 0;
                        }
                    }
                    return 3;
                }
                if (self.scared <= 0.8f)
                {
                    return 0;
                }
                return 1;
            }
            else if (obj is Spear spear && spear.bugSpear)
            {
                if (!(self.currentViolenceType == ScavengerAI.ViolenceType.Lethal) && !pickupDropInsteadOfWeaponSelection)
                    return 1;
                else
                    return 4;
            }
            else return orig(self, obj, pickupDropInsteadOfWeaponSelection);
        }

        private int CollectBugSpearAndFireEgg(On.ScavengerAI.orig_CollectScore_PhysicalObject_bool orig, ScavengerAI self, PhysicalObject obj, bool weaponFiltered)
        {
            if (self.scavenger.room != null)
            {
                SocialEventRecognizer.OwnedItemOnGround ownedItemOnGround = self.scavenger.room.socialEventRecognizer.ItemOwnership(obj);
                if (ownedItemOnGround != null && ownedItemOnGround.offeredTo != null && ownedItemOnGround.offeredTo != self.scavenger)
                {
                    return 0;
                }
            }
            if (!(weaponFiltered && self.NeedAWeapon))
            {
                if (obj is Spear spear && spear.bugSpear)
                    return 4;
                else if (obj is FireEgg egg)
                {
                    if (egg.activeCounter <= 0)
                        return 3;
                    else return 0;
                }
            }
            if (obj is Lantern)
                return 5;

            return orig(self, obj, weaponFiltered);
        }

        #endregion


        #region ScavKing Behaviour
        private void KingNotWaiting(On.Scavenger.orig_ctor orig, Scavenger self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.King && abstractCreature.Room.name != "LC_FINAL")
            {
                self.kingWaiting = false;
            }
        }

        private void ScavKingActCanGoIntoPipes(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Creature>("shortcutDelay"),
                x => x.MatchLdcI4(1));
            c.Index += 6;
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_0);
        }

        private void ScavSensesFlyingWeapon(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                MoveType.After,
                    x => x.MatchCallvirt<ArtificialIntelligence>("VisualContact")
                );
            c.EmitDelegate<Func<bool, bool>>((origRes) =>
            {
                if (ScavengerTweaksOptions.ScavSenses.Value)
                    return true;
                else
                    return origRes;
            });

        }

        private void ScavKingCanTravelLikeElites(On.StaticWorld.orig_InitStaticWorld orig)
        {
            orig();
            StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing).doesNotUseDens = false;
            StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing).forbidStandardShortcutEntry = false;
            StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing).usesCreatureHoles = false;
            StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing).usesRegionTransportation = false;
            StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing).roamBetweenRoomsChance = -1f;
            StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing).usesNPCTransportation =
                StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite).usesNPCTransportation;
            StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing).shortcutAversion =
                StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite).shortcutAversion;
            StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing).mappedNodeTypes =
                StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite).mappedNodeTypes;
            StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing).pathingPreferencesConnections =
                StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite).pathingPreferencesConnections;
            StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing).NPCTravelAversion =
                StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite).NPCTravelAversion;
        }

        private void ScavKingAbsAICanGoIntoPipes(On.ScavengerAbstractAI.orig_AbstractBehavior orig, ScavengerAbstractAI self, int time)
        {
            if (self is null || self.parent is null || self.parent.creatureTemplate is null)
            {
                orig(self, time);
                return;
            }

            if (self.parent.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing &&
                !(self.parent.realizedCreature is null) && !(self.parent.realizedCreature.room is null) &&
                self.parent.realizedCreature.room.abstractRoom.name != "LC_FINAL")
            {
                self.parent.creatureTemplate.type = MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite;
                orig(self, time);
                self.parent.creatureTemplate.type = MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing;
            }
            else
                orig(self, time);
        }

        private void TrickKingIntoSquadBehaviour(On.ScavengerAI.orig_DecideBehavior orig, ScavengerAI self)
        {
            if (self.scavenger.King && !(self.scavenger.room is null) && self.scavenger.room.abstractRoom.name != "LC_FINAL")
            {
                try
                {
                    self.scavenger.abstractCreature.creatureTemplate.type = MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite;
                    orig(self);
                }
                finally
                {
                    self.scavenger.abstractCreature.creatureTemplate.type = MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing;
                }
            }
            else
                orig(self);
        }

        private void TrickKingIntoSquad(On.ScavengerAI.orig_Update orig, ScavengerAI self)
        {
            if (self.scavenger.King && !(self.scavenger.room is null) && self.scavenger.room.abstractRoom.name != "LC_FINAL")
            {
                try
                {
                    self.scavenger.abstractCreature.creatureTemplate.type = MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite;
                    orig(self);
                }
                finally
                {
                    self.scavenger.abstractCreature.creatureTemplate.type = MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing;
                }

            }
            else
                orig(self);
        }

        #endregion


        #region StrongScavs
        private void StrongScavReplacementChance(ILContext il)
        {
            //Chance to replace ID with a regular ID.
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.Before,
                x => x.MatchLdarg(5));
            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldarg_2);
            c.Emit(OpCodes.Ldarg, 5);
            c.EmitDelegate<Func<World, CreatureTemplate, EntityID, EntityID>>((world, creatureTemplate, origID) =>
            {
                if (!(world is null) && !(world.game is null) && !(creatureTemplate is null) && creatureTemplate.TopAncestor().type ==
                CreatureTemplate.Type.Scavenger)
                {
                    float chance = ScavengerTweaksOptions.StrongScavChance.Value / 100f;
                    if (creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite)
                        chance = ScavengerTweaksOptions.StrongEliteChance.Value / 100f;
                    if (creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing)
                        chance = 1f;
                    if (UnityEngine.Random.value < chance)
                        return origID;
                    EntityID replaceID = world.game.GetNewID();
                    return replaceID;
                }
                else return origID;
            });
            c.Emit(OpCodes.Starg, 5);
        }

        #endregion

        #region Helper Methods

        private void ClearMemory()
        {
            //If you have any collections (lists, dictionaries, etc.)
            //Clear them here to prevent a memory leak
            //YourList.Clear();
        }
        private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            game = self;
        }


        #endregion
    }
}
