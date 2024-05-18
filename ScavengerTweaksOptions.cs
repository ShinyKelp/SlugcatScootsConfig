using BepInEx.Logging;
using Menu.Remix.MixedUI;
using System.Collections.Generic;
using UnityEngine;

namespace ScavengerTweaks
{
    public class ScavengerTweaksOptions : OptionInterface
    {

        //public static ScavengerVideoOptions instance = new ScavengerVideoOptions();

        /*
        public static readonly Configurable<float> dodgeSkill = instance.config.Bind<float>("dodgeSkill", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> meleeSkill = instance.config.Bind<float>("meleeSkill", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> midRangeSkill = instance.config.Bind<float>("midRangeSkill", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> blockingSkill = instance.config.Bind<float>("blockingSkill", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> reactionSkill = instance.config.Bind<float>("reactionSkill", 0f, new ConfigAcceptableRange<float>(0f, 10f));

        public static readonly Configurable<float> aggression = instance.config.Bind<float>("aggression", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> bravery = instance.config.Bind<float>("bravery", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> dominance = instance.config.Bind<float>("dominance", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> energy = instance.config.Bind<float>("energy", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> nervous = instance.config.Bind<float>("nervous", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> sympathy = instance.config.Bind<float>("sympathy", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        //*/

        public Configurable<float> dodgeSkill;// = instance.config.Bind<float>("dodgeSkill", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public Configurable<float> meleeSkill;// = instance.config.Bind<float>("meleeSkill", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public Configurable<float> midRangeSkill;// = instance.config.Bind<float>("midRangeSkill", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public Configurable<float> blockingSkill;// = instance.config.Bind<float>("blockingSkill", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public Configurable<float> reactionSkill;// = instance.config.Bind<float>("reactionSkill", 0f, new ConfigAcceptableRange<float>(0f, 10f));

        public Configurable<float> aggression;// = instance.config.Bind<float>("aggression", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public Configurable<float> bravery;// = instance.config.Bind<float>("bravery", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public Configurable<float> dominance;// = instance.config.Bind<float>("dominance", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public Configurable<float> energy;// = instance.config.Bind<float>("energy", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public Configurable<float> nervous;// = instance.config.Bind<float>("nervous", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public Configurable<float> sympathy;// = instance.config.Bind<float>("sympathy", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public Configurable<bool> scavSenses;// = instance.config.Bind<float>("sympathy", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        private Configurable<string> comboBoxConfig;
        private bool hasStrongScavs;
        private OpComboBox gearComboBox;
        public Configurable<float> healthMultiplier;
        public Configurable<bool> variableHealth;// = instance.config.Bind<float>("sympathy", 0f, new ConfigAcceptableRange<float>(0f, 10f));


        public int ScavengerGearLevel { 
            get
            {
                if (gearComboBox is null)
                {
                    if (gearComboBox is null)
                    {
                        List<ListItem> boxList = new List<ListItem>
                    {
                        new ListItem("Vanilla", 0),
                        new ListItem("Less Explosives", 1),
                        new ListItem("No Items", 2)
                    };
                        gearComboBox = new OpComboBox(comboBoxConfig, new Vector2(230f, 107f), 120, boxList);
                        gearComboBox.value = comboBoxConfig.Value;
                    }
                }
                return gearComboBox.GetIndex();
            }
        }

        public readonly Configurable<int> strongScavChance;
        public readonly Configurable<int> strongEliteChance;

        public static Configurable<int> StrongScavChance, StrongEliteChance;
        public static Configurable<bool> ScavSenses;

        public ScavengerTweaksOptions(bool hasStrongScavs = false)
        {
            dodgeSkill = config.Bind<float>("dodgeSkill", 0f, new ConfigAcceptableRange<float>(-1f, 2f));
            meleeSkill = config.Bind<float>("meleeSkill", 0f, new ConfigAcceptableRange<float>(-1f, 2f));
            midRangeSkill = config.Bind<float>("midRangeSkill", 0f, new ConfigAcceptableRange<float>(-1f, 2f));
            blockingSkill = config.Bind<float>("blockingSkill", 0f, new ConfigAcceptableRange<float>(-1f, 2f));
            reactionSkill = config.Bind<float>("reactionSkill", 0f, new ConfigAcceptableRange<float>(-1f, 2f));

            aggression = config.Bind<float>("aggression", 0f, new ConfigAcceptableRange<float>(-1f, 1f));
            bravery = config.Bind<float>("bravery", 0f, new ConfigAcceptableRange<float>(-1f, 1f));
            dominance = config.Bind<float>("dominance", 0f, new ConfigAcceptableRange<float>(-1f, 1f));
            energy = config.Bind<float>("energy", 0f, new ConfigAcceptableRange<float>(-1f, 1f));
            nervous = config.Bind<float>("nervous", 0f, new ConfigAcceptableRange<float>(-1f, 1f));
            sympathy = config.Bind<float>("sympathy", 0f, new ConfigAcceptableRange<float>(-1f, 1f));
            scavSenses = config.Bind<bool>("scavSenses", false);
            ScavSenses = scavSenses;
            ConfigAcceptableBase info = null;
            comboBoxConfig = config.Bind<string>("GearChoice", "Vanilla", info);
            this.hasStrongScavs = hasStrongScavs;
            strongScavChance = config.Bind<int>("StrongScavChance", 100, new ConfigAcceptableRange<int>(0, 100));
            strongEliteChance = config.Bind<int>("StrongEliteChance", 100, new ConfigAcceptableRange<int>(0, 100));
            StrongScavChance = strongScavChance;
            StrongEliteChance = strongEliteChance;

            healthMultiplier = config.Bind<float>("scavHealthMultiplier", 1f, new ConfigAcceptableRange<float>(0.5f, 4f));
            variableHealth = config.Bind<bool>("scavVariableHealth", false);

        }

        private UIelement[] UIArrPlayerOptions;

        public override void Initialize()
        {
            var opTab = new OpTab(this, "Options");
            
            if(gearComboBox is null)
            {
                List<ListItem> boxList = new List<ListItem>
                    {
                        new ListItem("Vanilla", 0),
                        new ListItem("Less Explosives", 1),
                        new ListItem("No Items", 2)
                    };
                gearComboBox = new OpComboBox(comboBoxConfig, new Vector2(360f, 82f), 120, boxList);
                gearComboBox.value = comboBoxConfig.Value;
            }


            UIArrPlayerOptions = new UIelement[]
            {
                new OpLabel(10f, 570f, "Options", true),

                new OpLabel(15f, 493f, "Scavenger fighting skill modifiers (additive):"),

                new OpLabel(15f, 470f, "Dodges (dodgeSkill)"),
                new OpFloatSlider(dodgeSkill,new Vector2(20f, 440f), 200),
                new OpLabel(15f, 410f, "Melee attacks (meleeSkill)"),
                new OpFloatSlider(meleeSkill,new Vector2(20f,380f), 200),
                new OpLabel(15f, 350f, "Mid range attacks (midRangeSkill)"),
                new OpFloatSlider(midRangeSkill,new Vector2(20f,320f), 200),
                new OpLabel(15f, 290f, "Parries (blockingSkill)"),
                new OpFloatSlider(blockingSkill,new Vector2(20f,260f), 200),
                new OpLabel(15f, 230f, "Reaction time (reactionSkill)"),
                new OpFloatSlider(reactionSkill,new Vector2(20f,200f), 200),


                new OpLabel(315f, 493f, "Scavenger personality modifiers (additive):"),

                new OpLabel(315f, 470f, "Aggression"),
                new OpFloatSlider(aggression,new Vector2(320f, 440f), 200),
                new OpLabel(315f, 410f, "Bravery"),
                new OpFloatSlider(bravery,new Vector2(320f,380f), 200),
                new OpLabel(315f, 350f, "Dominance"),
                new OpFloatSlider(dominance,new Vector2(320f,320f), 200),
                new OpLabel(315f, 290f, "Energy"),
                new OpFloatSlider(energy,new Vector2(320f,260f), 200),
                new OpLabel(315f, 230f, "Nervous"),
                new OpFloatSlider(nervous,new Vector2(320f,200f), 200),
                new OpLabel(315f, 170f, "Sympathy"),
                new OpFloatSlider(sympathy,new Vector2(320f,140f), 200),
                new OpLabel(15f, 145f, "Scav Senses"),
                new OpCheckBox(scavSenses, 100f, 142f){
                    description = "Scavengers will always sense spears flying at them,\n" +
                    "making their dodges a lot better."
                },
                 new OpLabel(15f, 115f, "Variable health"),
                new OpCheckBox(variableHealth, 100f, 112f){
                    description = "Scavenger health will vary depending on certain personality traits."
                },

                new OpLabel(315f, 110f, "Arena starting gear"),
                gearComboBox,

                new OpLabel(15f, 50f, "Health multiplier"),
                new OpFloatSlider(healthMultiplier,new Vector2(20f,20f), 200),
               
            };

            opTab.AddItems(UIArrPlayerOptions);
            if (hasStrongScavs)
            {
                var ssOpTab = new OpTab(this, "StrongScavs");
                ssOpTab.AddItems(new UIelement[]
                {
                    new OpLabel(95f, 475f, "Chance of strong scav"),
                    new OpUpdown(strongScavChance, new Vector2(15f, 470f), 60),
                    new OpLabel(95f, 440f, "Chance of elite strong scav"),
                    new OpUpdown(strongEliteChance, new Vector2(15f, 435f), 60),
                });
                this.Tabs = new[]
                {
                    opTab,
                    ssOpTab
                };
            }
            else
            {
                this.Tabs = new[]
                {
                    opTab
                };
            }
        }

    }
}