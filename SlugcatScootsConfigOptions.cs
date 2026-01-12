using BepInEx.Logging;
using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace SlugcatScootsConfig
{
    public class SlugcatScootsConfigOptions : OptionInterface
    {
        private readonly ManualLogSource Logger;

        public static SlugcatScootsConfigOptions instance = new SlugcatScootsConfigOptions();


        public static readonly Configurable<float> slidePounceX = instance.config.Bind<float>("SlidePounceX", -1f, new ConfigAcceptableRange<float>(-1f, 39f));
        public static readonly Configurable<float> slidePounceY = instance.config.Bind<float>("SlidePounceY", -1f, new ConfigAcceptableRange<float>(-1f, 39f));

        public static readonly Configurable<float> rollPounceMultiplierX = instance.config.Bind<float>("rollPounceMultX", -1f, new ConfigAcceptableRange<float>(-1f, 39f));
        public static readonly Configurable<float> rollPounceMultiplierY = instance.config.Bind<float>("rollPounceMultY", -1f, new ConfigAcceptableRange<float>(-1f, 39f));

        public static readonly Configurable<float> whiplashX = instance.config.Bind<float>("WhiplashX", -1, new ConfigAcceptableRange<float>(-1f, 39f));
        public static readonly Configurable<float> whiplashY = instance.config.Bind<float>("WhiplashY", -1, new ConfigAcceptableRange<float>(-1f, 39f));

        public static readonly Configurable<float> backflip = instance.config.Bind<float>("Backflip", -1f, new ConfigAcceptableRange<float>(-1f, 39f));
        public static readonly Configurable<float> backflipFloat = instance.config.Bind<float>("BackflipFloat", -1f, new ConfigAcceptableRange<float>(-1f, 39f));

        public static readonly Configurable<int> wallPounceX = instance.config.Bind<int>("WallPounceX", -1, new ConfigAcceptableRange<int>(-1, 100));
        public static readonly Configurable<int> wallPounceY = instance.config.Bind<int>("WallPounceY", -1, new ConfigAcceptableRange<int>(-1, 100));
        public static readonly Configurable<int> postWallPounceStun = instance.config.Bind<int>("WallPounceStun", -1, new ConfigAcceptableRange<int>(-1, 80));

        public static readonly Configurable<int> slideAcceleration = instance.config.Bind<int>("BellySlide", -1, new ConfigAcceptableRange<int>(-1, 100));
        public static readonly Configurable<int> extendedSlideAcceleration = instance.config.Bind<int>("LongBellySlide", -1, new ConfigAcceptableRange<int>(-1, 100));

        public static readonly Configurable<int> slideInitDuration = instance.config.Bind<int>("SlideInitDuration", -1, new ConfigAcceptableRange<int>(-1, 80));
        public static readonly Configurable<float> slideInitPushback = instance.config.Bind<float>("SlideInitPushback", -1f, new ConfigAcceptableRange<float>(-1f, 39f));

        public static readonly Configurable<int> slidePounceWindow = instance.config.Bind<int>("SlidePounceWindow", -1, new ConfigAcceptableRange<int>(-1, 80));
        public static readonly Configurable<int> extendedSlidePounceWindow = instance.config.Bind<int>("ExtendedSlidePounceWindow", -1, new ConfigAcceptableRange<int>(-1, 80));

        public static readonly Configurable<int> postSlideStun = instance.config.Bind<int>("PostSlideStun", -1, new ConfigAcceptableRange<int>(-1, 80));
        public static readonly Configurable<int> standingPostSlideStun = instance.config.Bind<int>("StandingPostSlideStun", -1, new ConfigAcceptableRange<int>(-1, 80));

        public static readonly Configurable<int> slideDuration = instance.config.Bind<int>("SlideDuration", -1, new ConfigAcceptableRange<int>(-1, 400));
        public static readonly Configurable<int> extendedSlideDuration = instance.config.Bind<int>("ExtendedSlideDuration", -1, new ConfigAcceptableRange<int>(-1, 400));

        public static readonly Configurable<int> rollDuration = instance.config.Bind<int>("RollCount", -1, new ConfigAcceptableRange<int>(-1, 800));
        public static readonly Configurable<float> rollSpeed = instance.config.Bind<float>("RollSpeed", -1f, new ConfigAcceptableRange<float>(-1f, 10f));

        public static readonly Configurable<float> poleBoostForce = instance.config.Bind<float>("PoleBoost", -1f, new ConfigAcceptableRange<float>(-1f, 39f));
        public static readonly Configurable<float> poleBoostRegression = instance.config.Bind<float>("PoleRegression", -1f, new ConfigAcceptableRange<float>(-1f, 39f));
        public static readonly Configurable<int> postPoleBoostStun = instance.config.Bind<int>("PostPoleBoostStun", -1, new ConfigAcceptableRange<int>(-1, 80));

        public static readonly Configurable<float> corridorBoostForce = instance.config.Bind<float>("CorridorBoost", -1f, new ConfigAcceptableRange<float>(-1f, 39f));
        public static readonly Configurable<float> failedCorridorBoostForce = instance.config.Bind<float>("FailedCorridorBoost", -1f, new ConfigAcceptableRange<float>(-1f, 39f));
        public static readonly Configurable<int> postCorridorBoostStun = instance.config.Bind<int>("PostCorridorBoostStun", -1, new ConfigAcceptableRange<int>(-1, 80));

        public static Configurable<bool> doubleJumpOverTriple = instance.config.Bind<bool>("DoubleJumpOverTriple", false);
        public static Configurable<float> middleJumpBoost = instance.config.Bind<float>("MiddleJumpBoost", -11f, new ConfigAcceptableRange<float>(-11f, 20f));
        public static Configurable<float> flipJumpBoost = instance.config.Bind<float>("FlipJumpBoost", -11f, new ConfigAcceptableRange<float>(-11f, 20f));
        //*/

        private const float labelSpacing = 35f, shortSpacing = 60f, longSpacing = 80f;
        private const float labelX = 28f, sliderX = 40f;
        private const int sliderLength = 400;

        private UIelement[] PresetOptions, JumpOptions, WallPounceOptions, SlideOptions, SlideOptions2, RollOptions, BoostOptions;
        private OpSimpleButton defaultJumps, defaultWallpounce, defaultSlide, defaultRoll, defaultBoosts;

        private Configurable<string> comboBoxConfig, presetTextConfig;
        private Dictionary<string, OpSlider> OpSliderRefs;
        private Dictionary<string, OpFloatSlider> OpFloatSliderRefs;
        private OpTextBox presetText;
        private OpComboBox presetsComboBox;
        private OpSimpleButton  savePresetButton, loadPresetButton, removePresetButton;

        public SlugcatScootsConfigOptions()
        {
            ConfigurableInfo info = null;
            comboBoxConfig = this.config.Bind<string>("PresetComboBox", "Default", info);
            presetTextConfig = this.config.Bind<string>(null, "", info);
            OpSliderRefs = new Dictionary<string, OpSlider>();
            OpFloatSliderRefs = new Dictionary<string, OpFloatSlider>();
        }
        public override void Initialize()
        {
            OpSliderRefs.Clear();
            OpFloatSliderRefs.Clear();
            List<ListItem> boxList = new List<ListItem>();
            ListItem def = new ListItem("Default");
            ListItem surv = new ListItem("Survivor");
            ListItem riv = new ListItem("Rivulet");
            ListItem gourm = new ListItem("Gourmand");

            boxList.Add(def);
            boxList.Add(surv);
            boxList.Add(riv);
            boxList.Add(gourm);

            if (Directory.Exists(Custom.RootFolderDirectory() + "/SlugcatScootsConfig"))
            {
                foreach (string filename in Directory.GetFiles(Custom.RootFolderDirectory() + "/SlugcatScootsConfig"))
                {
                    string[] splitted = filename.Split('\\');
                    string aloneName = splitted[splitted.Length - 1].Split('.')[0];
                    boxList.Add(new ListItem(aloneName));
                }
            }
            else
                Directory.CreateDirectory(Custom.RootFolderDirectory() + "/SlugcatScootsConfig");

            Vector2 presetsPos = new Vector2(220, 435);

            presetsComboBox = new OpComboBox(comboBoxConfig, presetsPos, 130, boxList);
            presetsPos.y += 32;
            presetText = new OpTextBox(presetTextConfig, presetsPos, 130f);

            presetsPos.x -= 95f;
            presetsPos.y -= 2f;

            savePresetButton = new OpSimpleButton(presetsPos, new Vector2(88, 28), "SAVE PRESET")
            {
                description = "Save preset (if text is empty, saves currently selected one)"
            };

            presetsPos.y -= 32f;
            loadPresetButton = new OpSimpleButton(presetsPos, new Vector2(88, 28), "LOAD PRESET")
            {
                description = "Load selected preset"
            };

            presetsPos.x += 234;
            removePresetButton = new OpSimpleButton(presetsPos, new Vector2(108, 30), "REMOVE PRESET")
            {
                description = "Remove selected preset"
            };
            loadPresetButton.OnClick += LoadPreset;
            savePresetButton.OnClick += SavePreset;
            removePresetButton.OnClick += RemovePreset;

            PresetOptions = new UIelement[]
            {
                new OpLabel(10f, 524f, "Options", true),
                presetsComboBox,
                presetText,
                loadPresetButton,
                savePresetButton,
                removePresetButton,
                new OpLabel(20f, 360f, "NOTE: Assigning the minimum value to a config\nwill DISABLE it entirely.")
            };

            var presetsTab = new OpTab(this, "Presets");
            var jumpTab = new OpTab(this, "Jumps");
            var wallPounceTab = new OpTab(this, "WallPounce");
            var slideTab = new OpTab(this, "Slide");
            var slideTab2 = new OpTab(this, "Slide 2");
            var rollTab = new OpTab(this, "Roll");
            var boostTab = new OpTab(this, "Boosts");
            var tripleJumpTab = new OpTab(this, "Triple Jump");
            

            defaultJumps = new OpSimpleButton(new Vector2(530f, 10f), new Vector2(60, 30), "Defaults")
            {
                description = "Reset Jump options."
            };
            defaultWallpounce = new OpSimpleButton(new Vector2(530f, 10f), new Vector2(60, 30), "Defaults")
            {
                description = "Reset Wallpounce options."
            };
            defaultRoll = new OpSimpleButton(new Vector2(530f, 10f), new Vector2(60, 30), "Defaults")
            {
                description = "Reset Roll options."
            };
            defaultSlide = new OpSimpleButton(new Vector2(530f, 10f), new Vector2(60, 30), "Defaults")
            {
                description = "Reset Slide options."
            };
            defaultBoosts = new OpSimpleButton(new Vector2(530f, 10f), new Vector2(60, 30), "Defaults")
            {
                description = "Reset Boost options."
            };
            defaultJumps.OnClick += (_) => { SetDefaultJumps(); };
            defaultWallpounce.OnClick += (_) => { SetDefaultWallpounce(); };
            defaultRoll.OnClick += (_) => { SetDefaultRoll(); };
            defaultSlide.OnClick += (_) => { SetDefaultSlide(); };
            defaultBoosts.OnClick += (_) => { SetDefaultBoosts(); };


            float firstHeight = 575f, secondHeight = 435f, thirdHeight = 295f, fourthHeight = 155f;

            JumpOptions = new UIelement[]
            {

                new OpLabel(labelX, firstHeight, "Slide Pounce X. Normal: 9, Rivulet: 18."),
                new OpFloatSlider(slidePounceX, new Vector2(sliderX, firstHeight - labelSpacing), sliderLength),
                new OpLabel(labelX, firstHeight - shortSpacing, "Slide Pounce Y. Normal: 8.5, Rivulet: 10."),
                new OpFloatSlider(slidePounceY, new Vector2(sliderX, firstHeight - shortSpacing - labelSpacing), sliderLength),

                new OpLabel(labelX, secondHeight, "Roll Pounce X (Multiplier). Normal: 1, Rivulet: 1.5."),
                new OpFloatSlider(rollPounceMultiplierX, new Vector2(sliderX, secondHeight - labelSpacing), sliderLength),
                new OpLabel(labelX, secondHeight - shortSpacing, "Roll Pounce Y (Multiplier). Normal: 1. Rivulet: 1.1*."),
                new OpFloatSlider(rollPounceMultiplierY, new Vector2(sliderX, secondHeight - shortSpacing - labelSpacing), sliderLength)
                {
                    description = "Rivulet has a slight built-in increase to this value, regardless of the multiplier."
                },

                new OpLabel(labelX, thirdHeight, "Whiplash X. Normal: 7, Rivulet: 11."),
                new OpFloatSlider(whiplashX, new Vector2(sliderX, thirdHeight - labelSpacing), sliderLength),
                new OpLabel(labelX, thirdHeight - shortSpacing, "Whiplash Y. Normal: 10, Rivulet: 12."),
                new OpFloatSlider(whiplashY, new Vector2(sliderX, thirdHeight - shortSpacing - labelSpacing), sliderLength),

                new OpLabel(labelX, fourthHeight, "Backflip. Normal: 7, Rivulet: 10."),
                new OpFloatSlider(backflip, new Vector2(sliderX, fourthHeight - labelSpacing), sliderLength),
                new OpLabel(labelX, fourthHeight - shortSpacing, "Backflip floatiness. Normal: 5, Rivulet: 9."),
                new OpFloatSlider(backflipFloat, new Vector2(sliderX, fourthHeight - shortSpacing - labelSpacing), sliderLength)
                {
                    description = "Holding JUMP will make the jump floatier for longer.\nEquivalent to Slugcat Stats Config's jump boost modifier, but only applied to the backflip."
                },
                defaultJumps
            };
            SaveElementRefs(JumpOptions);

            SlideOptions = new UIelement[]
            {
                new OpLabel(labelX, firstHeight, "Slide acceleration. Normal: 18, Rivulet: 25, Gourmand: 45."),
                new OpSlider(slideAcceleration, new Vector2(sliderX, firstHeight - labelSpacing), sliderLength),
                new OpLabel(labelX, firstHeight - shortSpacing, "Extended Slide acceleration. Normal: 14, Rivulet: 20, Gourmand: 40."),
                new OpSlider(extendedSlideAcceleration, new Vector2(sliderX, firstHeight - shortSpacing - labelSpacing), sliderLength),

                new OpLabel(labelX, secondHeight, "Slide total duration. Normal: 15."),
                new OpSlider(slideDuration, new Vector2(sliderX, secondHeight - labelSpacing), sliderLength)
                {
                    description = "In frames. 1 second = 40 frames."
                },
                new OpLabel(labelX, secondHeight - shortSpacing, "Extended Slide total duration. Normal: 39."),
                new OpSlider(extendedSlideDuration, new Vector2(sliderX, secondHeight - shortSpacing - labelSpacing), sliderLength),


                new OpLabel(labelX, thirdHeight, "Initial Slide prep duration. Normal: 6, Rivulet: 0*."),
                new OpSlider(slideInitDuration, new Vector2(sliderX, thirdHeight - labelSpacing), sliderLength)
                {
                    description = "Initial hop animation of the slide. Rivulet always skips it."
                },
                new OpLabel(labelX, thirdHeight - shortSpacing, "Initial Slide pushback. Normal: 9.1."),
                new OpFloatSlider(slideInitPushback, new Vector2(sliderX, thirdHeight - shortSpacing - labelSpacing), sliderLength)
                {
                    description = "Negative force applied to slugcat during the initial hop animation."
                },
                defaultSlide
            };
            SaveElementRefs(SlideOptions);

            SlideOptions2 = new UIelement[]
            {

                new OpLabel(labelX, firstHeight, "Post-Slide stun duration. Normal: 40."),
                new OpSlider(postSlideStun, new Vector2(sliderX, firstHeight - labelSpacing), sliderLength)
                {
                    description = "In frames. 1 second = 40 frames. Slugcat will move slower for this time after finishing a slide and not standing (ex: a failed slide)."
                },
                new OpLabel(labelX, firstHeight - shortSpacing, "Post-Slide stun duration, if slugcat is standing at the end. Normal: 20."),
                new OpSlider(standingPostSlideStun, new Vector2(sliderX, firstHeight - shortSpacing - labelSpacing), sliderLength)
                {
                    description = "In frames. 1 second = 40 frames. Slugcat will move slower for this time after finishing a slide and standing up."
                },

                new OpLabel(labelX, secondHeight, "Earliest slide pounce frame. Normal: 12, Rivulet: 6."),
                new OpSlider(slidePounceWindow, new Vector2(sliderX, secondHeight - labelSpacing), sliderLength)
                {
                    description = "In frames. 1 second = 40 frames.\nCAUTION: These values should not be higher than their respective slide durations, or pounces won't be possible!"
                },
                new OpLabel(labelX, secondHeight - shortSpacing, "Earliest extended slide pounce frame. Normal: 34, Rivulet: 20."),
                new OpSlider(extendedSlidePounceWindow, new Vector2(sliderX, secondHeight - shortSpacing - labelSpacing), sliderLength)
                {
                    description = "In frames. 1 second = 40 frames.\nCAUTION: These values should not be higher than their respective slide durations, or pounces won't be possible!"
                }
            };
            SaveElementRefs(SlideOptions2);

            secondHeight = firstHeight - shortSpacing * 2 - longSpacing;

            WallPounceOptions = new UIelement[]
            {
                new OpLabel(labelX, firstHeight, "Wallpounce X. Normal: 17, Rivulet: 25."),
                new OpSlider(wallPounceX, new Vector2(sliderX, firstHeight - labelSpacing), sliderLength),
                new OpLabel(labelX, firstHeight-shortSpacing, "Wallpounce Y. Normal: 10, Rivulet: 10."),
                new OpSlider(wallPounceY, new Vector2(sliderX, firstHeight - shortSpacing - labelSpacing), sliderLength),
                new OpLabel(labelX, firstHeight-shortSpacing*2, "Post-Wallpounce stun. Normal: 20, Rivulet: 15."),
                new OpSlider(postWallPounceStun, new Vector2(sliderX, firstHeight - shortSpacing*2 - labelSpacing), sliderLength)
                {
                    description = "Time period after a Wallpounce in which slugcat won't be able to drift in the air."
                },
                defaultWallpounce
            };
            SaveElementRefs(WallPounceOptions);


            RollOptions = new UIelement[]
            {
                new OpLabel(labelX, firstHeight, "Rolling duration. Normal: 15, Gourmand: Infinite*"),
                new OpSlider(rollDuration, new Vector2(sliderX, firstHeight - labelSpacing), sliderLength)
                {
                    description = "Gourmand can roll as long as they don't run out of stamina. This option won't affect them."
                },
                new OpLabel(labelX, firstHeight - shortSpacing, "Rolling speed (Addition). Base value: 1.1."),
                new OpFloatSlider(rollSpeed, new Vector2(sliderX, firstHeight - shortSpacing - labelSpacing), sliderLength)
                {
                    description = "This value will be added to the base roll speed. Zero means unchanged.\nCAUTION: High values get buggy in slanted surfaces."
                },
                defaultRoll
            };
            SaveElementRefs(RollOptions);


            BoostOptions = new UIelement[]
            {
                new OpLabel(labelX, firstHeight, "Pole Boost force. Normal: 3."),
                new OpFloatSlider(poleBoostForce, new Vector2(sliderX, firstHeight - labelSpacing), sliderLength),
                new OpLabel(labelX, firstHeight - shortSpacing, "Pole Boost regression. Normal: 1.2."),
                new OpFloatSlider(poleBoostRegression, new Vector2(sliderX, firstHeight - shortSpacing - labelSpacing), sliderLength)
                {
                    description = "Negative force applied to slugcat after a pole boost."
                },
                new OpLabel(labelX, firstHeight - shortSpacing*2, "Post-Pole Boost stun duration. Normal: 16."),
                new OpSlider(postPoleBoostStun, new Vector2(sliderX, firstHeight - shortSpacing*2 - labelSpacing), sliderLength)
                {
                    description = "In frames. 1 second = 40 frames. Happens AFTER regression force, not during it."
                },

                new OpLabel(labelX, secondHeight, "Corridor Boost force (Addition). Base: 7."),
                new OpFloatSlider(corridorBoostForce, new Vector2(sliderX, secondHeight - labelSpacing), sliderLength)
                {
                    description = "This force is added to the base value upon a successful Corridor Boost."
                },
                new OpLabel(labelX, secondHeight - shortSpacing, "Failed Corridor Boost force (Addition). Base: 5."),
                new OpFloatSlider(failedCorridorBoostForce, new Vector2(sliderX, secondHeight - shortSpacing - labelSpacing), sliderLength)
                {
                    description = "This force is added to the base value upon a failed Corridor Boost."
                },
                new OpLabel(labelX, secondHeight - shortSpacing*2, "Post-Corridor Boost stun duration. Normal: 15."),
                new OpSlider(postCorridorBoostStun, new Vector2(sliderX, secondHeight - shortSpacing*2 - labelSpacing), sliderLength)
                {
                    description = "In frames. 1 second = 40 frames. Applies equally to successful or failed boosts."
                },
                defaultBoosts
            };
            SaveElementRefs(BoostOptions);

            secondHeight = firstHeight - shortSpacing;
            thirdHeight = secondHeight - shortSpacing;


            if (SlugcatScootsConfigMod.hasTripleJump)
                this.Tabs = new[]
                    {
                        presetsTab,
                        jumpTab,
                        slideTab,
                        slideTab2,
                        rollTab,
                        wallPounceTab,
                        boostTab,
                        tripleJumpTab
                    };
            else
                this.Tabs = new[]
                    {
                        presetsTab,
                        jumpTab,
                        slideTab,
                        slideTab2,
                        rollTab,
                        wallPounceTab,
                        boostTab
                    };

            presetsTab.AddItems(PresetOptions);
            jumpTab.AddItems(JumpOptions);
            wallPounceTab.AddItems(WallPounceOptions);
            slideTab.AddItems(SlideOptions);
            slideTab2.AddItems(SlideOptions2);
            rollTab.AddItems(RollOptions);
            boostTab.AddItems(BoostOptions);
            if (SlugcatScootsConfigMod.hasTripleJump)
            {
                UIelement[] tripleJumpOptions = new UIelement[]
                {
                    new OpCheckBox(doubleJumpOverTriple, new Vector2(sliderX, firstHeight - 20)),
                    new OpLabel(sliderX + 28f, firstHeight - 20 + 3, "Flip will happen on second jump (unchecked by default)"),
                    new OpLabel(labelX, secondHeight, "Middle Jump Boost. Default: 1.5"),
                    new OpFloatSlider(middleJumpBoost, new Vector2(sliderX, secondHeight - labelSpacing), sliderLength),
                    new OpLabel(labelX, thirdHeight, "Flip Jump Boost. Default: 3"),
                    new OpFloatSlider(flipJumpBoost, new Vector2(sliderX, thirdHeight - labelSpacing), sliderLength)
                };
                tripleJumpTab.AddItems(tripleJumpOptions);
                SaveElementRefs(tripleJumpOptions);
            }

        }

        private void SetDefaultJumps()
        {
            for (int i = 0; i < JumpOptions.Length; i++)
                if (JumpOptions[i] is OpSlider op)
                {
                    op.Reset();
                }
                else if(JumpOptions[i] is OpFloatSlider opF)
                {
                    opF.Reset();
                }
        }

        private void SetDefaultWallpounce()
        {
            for (int i = 0; i < WallPounceOptions.Length; i++)
                if (WallPounceOptions[i] is OpSlider op)
                {
                    op.Reset();
                }
                else if (WallPounceOptions[i] is OpFloatSlider opF)
                {
                    opF.Reset();
                }
        }

        private void SetDefaultSlide()
        {
            for (int i = 0; i < SlideOptions.Length; i++)
                if (SlideOptions[i] is OpSlider op)
                {
                    op.Reset();
                }
                else if (SlideOptions[i] is OpFloatSlider opF)
                {
                    opF.Reset();
                }

            for (int i = 0; i < SlideOptions2.Length; i++)
                if (SlideOptions2[i] is OpSlider op)
                {
                    op.Reset();
                }
                else if (SlideOptions2[i] is OpFloatSlider opF)
                {
                    opF.Reset();
                }
        }

        private void SetDefaultRoll()
        {
            for (int i = 0; i < RollOptions.Length; i++)
                if (RollOptions[i] is OpSlider op)
                {
                    op.Reset();
                }
                else if (RollOptions[i] is OpFloatSlider opF)
                {
                    opF.Reset();
                }
        }

        private void SetDefaultBoosts()
        {
            for (int i = 0; i < BoostOptions.Length; i++)
                if (BoostOptions[i] is OpSlider op)
                {
                    op.Reset();
                }
                else if (BoostOptions[i] is OpFloatSlider opF)
                {
                    opF.Reset();
                }
        }

        #region Presets

        void SaveElementRefs(UIelement[] elements)
        {
            foreach (UIelement uie in elements)
                if (uie is OpSlider ops)
                    OpSliderRefs.Add(ops.Key, ops);
                else if (uie is OpFloatSlider opfs)
                    OpFloatSliderRefs.Add(opfs.Key, opfs);
        }
        private void SavePreset(UIfocusable trigger)
        {
            string presetName = presetText.value;
            if (presetName == "" || presetName is null || presetName == string.Empty)
                presetName = presetsComboBox.GetItemList()[presetsComboBox.GetIndex()].name;
            if (presetName == "Default" || presetName == "Survivor" || presetName == "Rivulet" || presetName == "Gourmand")
                return;
            if (!Directory.Exists(Custom.RootFolderDirectory() + "/SlugcatScootsConfig"))
                Directory.CreateDirectory(Custom.RootFolderDirectory() + "/SlugcatScootsConfig");

            string filePath = Custom.RootFolderDirectory() + "/SlugcatScootsConfig/" + presetName + ".txt";
            Dictionary<string, int> valuesToSave = new Dictionary<string, int>();
            Dictionary<string, float> valuesToSave2 = new Dictionary<string, float>();
            bool alreadyExists;
            if (alreadyExists = File.Exists(filePath))
            {
                StreamReader sr = new StreamReader(filePath);
                string line = sr.ReadLine();
                while (line != null)
                {
                    string[] splits = line.Split('|');
                    if (splits[1].EndsWith("f"))
                    {
                        valuesToSave2.Add(splits[0], float.Parse(splits[1].Substring(0,splits[1].Length - 1)));
                    }
                    else
                    {
                        valuesToSave.Add(splits[0], int.Parse(splits[1]));
                    }
                    line = sr.ReadLine();
                }
                sr.Close();
            }

            foreach (KeyValuePair<string, OpSlider> pair in OpSliderRefs)
            {
                if (valuesToSave.ContainsKey(pair.Key))
                    valuesToSave[pair.Key] = Int32.Parse(pair.Value.value);
                else
                    valuesToSave.Add(pair.Key, Int32.Parse(pair.Value.value));
            }

            foreach (KeyValuePair<string, OpFloatSlider> pair in OpFloatSliderRefs)
            {
                if (valuesToSave2.ContainsKey(pair.Key))
                    valuesToSave2[pair.Key] = float.Parse(pair.Value.value);
                else
                    valuesToSave2.Add(pair.Key, float.Parse(pair.Value.value));
            }

            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                foreach (KeyValuePair<string, int> pair in valuesToSave)
                    writer.WriteLine(pair.Key + "|" + pair.Value);
                foreach (KeyValuePair<string, float> pair in valuesToSave2)
                    writer.WriteLine(pair.Key + "|" + pair.Value + "f");
            }

            if (!alreadyExists)
                presetsComboBox.AddItems(false, new ListItem(presetName));
            presetText.Reset();
        }

        private void LoadPreset(UIfocusable trigger)
        {
            string presetName = presetsComboBox.GetItemList()[presetsComboBox.GetIndex()].name;
            if (presetName == "Default")
                SetDefaults();
            else if(presetName == "Survivor")
            {
                SetSurvivorOptions();
            }
            else if (presetName == "Rivulet")
            {
                SetRivuletOptions();
            }
            else if (presetName == "Gourmand")
            {
                SetGourmandOptions();
            }
            else
            {
                StreamReader sr = new StreamReader(Custom.RootFolderDirectory() + "/SlugcatScootsConfig/" + presetName + ".txt");
                string line = sr.ReadLine();
                while (line != null)
                {
                    string[] splits = line.Split('|');
                    if (OpSliderRefs.ContainsKey(splits[0]))
                        SetValue(OpSliderRefs[splits[0]], Int32.Parse(splits[1]));
                    else if (OpFloatSliderRefs.ContainsKey(splits[0]))
                        SetValue(OpFloatSliderRefs[splits[0]], float.Parse(splits[1].Substring(0, splits[1].Length-1)));
                    line = sr.ReadLine();
                }
                sr.Close();
            }
        }

        private void RemovePreset(UIfocusable trigger)
        {
            string presetName = presetsComboBox.GetItemList()[presetsComboBox.GetIndex()].name;
            if (presetName == "Default" || presetName == "Survivor" || presetName == "Rivulet" || presetName == "Gourmand")
                return;
            presetsComboBox.RemoveItems(true, presetName);
            File.Delete(Custom.RootFolderDirectory() + "/SlugcatScootsConfig/" + presetName + ".txt");
        }

        private void SetSurvivorOptions()
        {
            SetConfigValue(slidePounceX.BoundUIconfig, 9);
            SetConfigValue(slidePounceY.BoundUIconfig, 8.5f);
            SetConfigValue(rollPounceMultiplierX.BoundUIconfig, 1f);
            SetConfigValue(rollPounceMultiplierY.BoundUIconfig, 1f);
            SetConfigValue(whiplashX.BoundUIconfig, 7);
            SetConfigValue(whiplashY.BoundUIconfig, 10);
            SetConfigValue(backflip.BoundUIconfig, 7);
            SetConfigValue(backflipFloat.BoundUIconfig, 5);
            SetConfigValue(slideAcceleration.BoundUIconfig, 18);
            SetConfigValue(extendedSlideAcceleration.BoundUIconfig, 14);
            SetConfigValue(slideDuration.BoundUIconfig, 15);
            SetConfigValue(extendedSlideDuration.BoundUIconfig, 39);
            SetConfigValue(slideInitDuration.BoundUIconfig, 6);
            SetConfigValue(slideInitPushback.BoundUIconfig, 9.1f);
            SetConfigValue(postSlideStun.BoundUIconfig, 40);
            SetConfigValue(standingPostSlideStun.BoundUIconfig, 20);
            SetConfigValue(slidePounceWindow.BoundUIconfig, 12);
            SetConfigValue(extendedSlidePounceWindow.BoundUIconfig, 34);
            SetConfigValue(wallPounceX.BoundUIconfig, 17);
            SetConfigValue(wallPounceY.BoundUIconfig, 10);
            SetConfigValue(postWallPounceStun.BoundUIconfig, 20);
            SetConfigValue(rollDuration.BoundUIconfig, 15);
            SetConfigValue(rollSpeed.BoundUIconfig, 0);
            SetConfigValue(poleBoostForce.BoundUIconfig, 3);
            SetConfigValue(poleBoostRegression.BoundUIconfig, 1.2f);
            SetConfigValue(postPoleBoostStun.BoundUIconfig, 16);
            SetConfigValue(corridorBoostForce.BoundUIconfig, 0);
            SetConfigValue(failedCorridorBoostForce.BoundUIconfig, 0);
            SetConfigValue(postCorridorBoostStun.BoundUIconfig, 15);
        }

        private void SetRivuletOptions()
        {
            SetConfigValue(slidePounceX.BoundUIconfig, 18);
            SetConfigValue(slidePounceY.BoundUIconfig, 10);
            SetConfigValue(rollPounceMultiplierX.BoundUIconfig, 1.5f);
            SetConfigValue(rollPounceMultiplierY.BoundUIconfig, 1.1f);
            SetConfigValue(whiplashX.BoundUIconfig, 11);
            SetConfigValue(whiplashY.BoundUIconfig, 12);
            SetConfigValue(backflip.BoundUIconfig, 10);
            SetConfigValue(backflipFloat.BoundUIconfig, 9);
            SetConfigValue(slideAcceleration.BoundUIconfig, 25);
            SetConfigValue(extendedSlideAcceleration.BoundUIconfig, 20);
            SetConfigValue(slideDuration.BoundUIconfig, 15);
            SetConfigValue(extendedSlideDuration.BoundUIconfig, 39);
            SetConfigValue(slideInitDuration.BoundUIconfig, 0);
            SetConfigValue(slideInitPushback.BoundUIconfig, 9.1f);
            SetConfigValue(postSlideStun.BoundUIconfig, 40);
            SetConfigValue(standingPostSlideStun.BoundUIconfig, 20);
            SetConfigValue(slidePounceWindow.BoundUIconfig, 6);
            SetConfigValue(extendedSlidePounceWindow.BoundUIconfig, 20);
            SetConfigValue(wallPounceX.BoundUIconfig, 25);
            SetConfigValue(wallPounceY.BoundUIconfig, 10);
            SetConfigValue(postWallPounceStun.BoundUIconfig, 15);
            SetConfigValue(rollDuration.BoundUIconfig, 15);
            SetConfigValue(rollSpeed.BoundUIconfig, 0);
            SetConfigValue(poleBoostForce.BoundUIconfig, 3);
            SetConfigValue(poleBoostRegression.BoundUIconfig, 1.2f);
            SetConfigValue(postPoleBoostStun.BoundUIconfig, 16);
            SetConfigValue(corridorBoostForce.BoundUIconfig, 0);
            SetConfigValue(failedCorridorBoostForce.BoundUIconfig, 0);
            SetConfigValue(postCorridorBoostStun.BoundUIconfig, 15);
        }

        private void SetGourmandOptions()
        {
            SetConfigValue(slidePounceX.BoundUIconfig, 9);
            SetConfigValue(slidePounceY.BoundUIconfig, 8.5f);
            SetConfigValue(rollPounceMultiplierX.BoundUIconfig, 1f);
            SetConfigValue(rollPounceMultiplierY.BoundUIconfig, 1f);
            SetConfigValue(whiplashX.BoundUIconfig, 7);
            SetConfigValue(whiplashY.BoundUIconfig, 10);
            SetConfigValue(backflip.BoundUIconfig, 7);
            SetConfigValue(backflipFloat.BoundUIconfig, 5);
            SetConfigValue(slideAcceleration.BoundUIconfig, 45);
            SetConfigValue(extendedSlideAcceleration.BoundUIconfig, 40);
            SetConfigValue(slideDuration.BoundUIconfig, 15);
            SetConfigValue(extendedSlideDuration.BoundUIconfig, 39);
            SetConfigValue(slideInitDuration.BoundUIconfig, 6);
            SetConfigValue(slideInitPushback.BoundUIconfig, 9.1f);
            SetConfigValue(postSlideStun.BoundUIconfig, 40);
            SetConfigValue(standingPostSlideStun.BoundUIconfig, 20);
            SetConfigValue(slidePounceWindow.BoundUIconfig, 12);
            SetConfigValue(extendedSlidePounceWindow.BoundUIconfig, 34);
            SetConfigValue(wallPounceX.BoundUIconfig, 17);
            SetConfigValue(wallPounceY.BoundUIconfig, 10);
            SetConfigValue(postWallPounceStun.BoundUIconfig, 20);
            SetConfigValue(rollDuration.BoundUIconfig, 800);
            SetConfigValue(rollSpeed.BoundUIconfig, 0);
            SetConfigValue(poleBoostForce.BoundUIconfig, 3);
            SetConfigValue(poleBoostRegression.BoundUIconfig, 1.2f);
            SetConfigValue(postPoleBoostStun.BoundUIconfig, 16);
            SetConfigValue(corridorBoostForce.BoundUIconfig, 0);
            SetConfigValue(failedCorridorBoostForce.BoundUIconfig, 0);
            SetConfigValue(postCorridorBoostStun.BoundUIconfig, 15);
        }

        private void SetDefaults()
        {
            SetDefaultJumps();
            SetDefaultSlide();
            SetDefaultWallpounce();
            SetDefaultRoll();
            SetDefaultBoosts();
            if (SlugcatScootsConfigMod.hasTripleJump)
            {
                SetConfigValue(middleJumpBoost.BoundUIconfig,-11f);
                SetConfigValue(flipJumpBoost.BoundUIconfig,-11f);
            }
        }

        private void SetConfigValue(UIconfig op, float value)
        {
            if (op is OpSlider ops)
                SetValue(ops, (int)value);
            else if(op is OpFloatSlider opfs)
                SetValue(opfs, value);
        }

        private void SetValue(OpSlider op, int value)
        {
            string aux = op.defaultValue;
            op.defaultValue = value.ToString();
            op.Reset();
            op.defaultValue = aux;
        }

        private void SetValue(OpFloatSlider op, float value)
        {
            string aux = op.defaultValue;
            op.defaultValue = value.ToString();
            op.Reset();
            op.defaultValue = aux;
        }

        #endregion
    }
}