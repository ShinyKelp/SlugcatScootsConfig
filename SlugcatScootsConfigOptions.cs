using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace SlugcatScootsConfig
{
    public class SlugcatScootsConfigOptions : OptionInterface
    {
        private readonly ManualLogSource Logger;

        public static SlugcatScootsConfigOptions instance = new SlugcatScootsConfigOptions();


        public static readonly Configurable<float> slidePounceX = instance.config.Bind<float>("SlidePounceX", 9f, new ConfigAcceptableRange<float>(0f, 40f));
        public static readonly Configurable<float> slidePounceY = instance.config.Bind<float>("SlidePounceY", 8.5f, new ConfigAcceptableRange<float>(0f, 40f));

        public static readonly Configurable<float> rollPounceMultiplierX = instance.config.Bind<float>("rollPounceMultX", 1, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> rollPounceMultiplierY = instance.config.Bind<float>("rollPounceMultY", 1, new ConfigAcceptableRange<float>(0f, 10f));

        public static readonly Configurable<float> whiplashX = instance.config.Bind<float>("WhiplashX", 7, new ConfigAcceptableRange<float>(0f, 40f));
        public static readonly Configurable<float> whiplashY = instance.config.Bind<float>("WhiplashY", 10, new ConfigAcceptableRange<float>(0f, 40f));

        public static readonly Configurable<float> backflip = instance.config.Bind<float>("Backflip", 7, new ConfigAcceptableRange<float>(0f, 40f));
        public static readonly Configurable<float> backflipFloat = instance.config.Bind<float>("BackflipFloat", 5, new ConfigAcceptableRange<float>(0f, 40f));

        public static readonly Configurable<int> wallPounceX = instance.config.Bind<int>("WallPounceX", 17, new ConfigAcceptableRange<int>(0, 100));
        public static readonly Configurable<int> wallPounceY = instance.config.Bind<int>("WallPounceY", 10, new ConfigAcceptableRange<int>(0, 100));
        public static readonly Configurable<float> wallPounceStun = instance.config.Bind<float>("WallPounceStun", 20f, new ConfigAcceptableRange<float>(0f, 40f));

        public static readonly Configurable<int> bellySlide = instance.config.Bind<int>("BellySlide", 18, new ConfigAcceptableRange<int>(0, 100));
        public static readonly Configurable<int> longBellySlide = instance.config.Bind<int>("LongBellySlide", 14, new ConfigAcceptableRange<int>(0, 100));

        public static readonly Configurable<int> slideInit = instance.config.Bind<int>("SlideInitDuration", 6, new ConfigAcceptableRange<int>(0, 80));
        public static readonly Configurable<float> slideInitPushback = instance.config.Bind<float>("SlideInitPushback", 9.1f, new ConfigAcceptableRange<float>(0f, 40f));

        public static readonly Configurable<int> slidePounceWindow = instance.config.Bind<int>("SlidePounceWindow", 12, new ConfigAcceptableRange<int>(0, 80));
        public static readonly Configurable<int> extendedSlidePounceWindow = instance.config.Bind<int>("ExtendedSlidePounceWindow", 34, new ConfigAcceptableRange<int>(0, 80));

        public static readonly Configurable<int> postSlideStun = instance.config.Bind<int>("PostSlideStun", 20, new ConfigAcceptableRange<int>(0, 80));
        public static readonly Configurable<int> extendedPostSlideStun = instance.config.Bind<int>("PostExtendedSlideStun", 40, new ConfigAcceptableRange<int>(0, 80));


        public static readonly Configurable<int> slideDuration = instance.config.Bind<int>("SlideDuration", 15, new ConfigAcceptableRange<int>(0, 400));
        public static readonly Configurable<int> extendedSlideDuration = instance.config.Bind<int>("ExtendedSlideDuration", 39, new ConfigAcceptableRange<int>(0, 400));


        public static readonly Configurable<int> rollCount = instance.config.Bind<int>("RollCount", 15, new ConfigAcceptableRange<int>(0, 800));
        public static readonly Configurable<float> rollSpeed = instance.config.Bind<float>("RollSpeed", 0f, new ConfigAcceptableRange<float>(-1f, 10f));


        //*/

        private const float labelSpacing = 35f, shortSpacing = 60f, longSpacing = 80f;
        private const float labelX = 28f, sliderX = 40f;
        private const int sliderLength = 400;

        private UIelement[] JumpOptions, WallPounceOptions, SlideOptions, SlideOptions2, RollOptions;
        private OpSimpleButton defaultJumps, defaultWallpounce, defaultSlide, defaultRoll;
        public override void Initialize()
        {
            var jumpTab = new OpTab(this, "Jumps");
            var wallPounceTab = new OpTab(this, "WallPounce");
            var slideTab = new OpTab(this, "Slide");
            var slideTab2 = new OpTab(this, "Slide 2");
            var rollTab = new OpTab(this, "Roll");
            this.Tabs = new[]
            {
                jumpTab,
                slideTab,
                slideTab2,
                rollTab,
                wallPounceTab
            };

            defaultJumps = new OpSimpleButton(new Vector2(530f, 10f), new Vector2(60, 30), "Defaults")
            {
                description = "Set Jumps options to default (survivor's stats)"
            };
            defaultWallpounce = new OpSimpleButton(new Vector2(530f, 10f), new Vector2(60, 30), "Defaults")
            {
                description = "Set Wallpounce options to default (survivor's stats)"
            };
            defaultRoll = new OpSimpleButton(new Vector2(530f, 10f), new Vector2(60, 30), "Defaults")
            {
                description = "Set Roll options to default (survivor's stats)"
            };
            defaultSlide = new OpSimpleButton(new Vector2(530f, 10f), new Vector2(60, 30), "Defaults")
            {
                description = "Set Slide options to default (survivor's stats)"
            };


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

            SlideOptions = new UIelement[]
            {
                new OpLabel(labelX, firstHeight, "Slide acceleration. Normal: 18, Rivulet: 25, Gourmand: 45."),
                new OpSlider(bellySlide, new Vector2(sliderX, firstHeight - labelSpacing), sliderLength),
                new OpLabel(labelX, firstHeight - shortSpacing, "Extended Slide acceleration. Normal: 14, Rivulet: 20, Gourmand: 40."),
                new OpSlider(longBellySlide, new Vector2(sliderX, firstHeight - shortSpacing - labelSpacing), sliderLength),

                new OpLabel(labelX, secondHeight, "Slide total duration. Normal: 15."),
                new OpSlider(slideDuration, new Vector2(sliderX, secondHeight - labelSpacing), sliderLength)
                {
                    description = "In frames. 1 second = 40 frames."
                },
                new OpLabel(labelX, secondHeight - shortSpacing, "Extended Slide total duration. Normal: 39."),
                new OpSlider(extendedSlideDuration, new Vector2(sliderX, secondHeight - shortSpacing - labelSpacing), sliderLength),


                new OpLabel(labelX, thirdHeight, "Initial Slide prep duration. Normal: 6, Rivulet: 0*."),
                new OpSlider(slideInit, new Vector2(sliderX, thirdHeight - labelSpacing), sliderLength)
                {
                    description = "Initial hop animation of the slide. Rivulet always skips it."
                },
                new OpLabel(labelX, thirdHeight - shortSpacing, "Initial Slide pushback. Default: 9.1."),
                new OpFloatSlider(slideInitPushback, new Vector2(sliderX, thirdHeight - shortSpacing - labelSpacing), sliderLength)
                {
                    description = "Negative force applied to slugcat during the initial hop animation."
                },
                defaultSlide
            };

            SlideOptions2 = new UIelement[]
            {

                new OpLabel(labelX, firstHeight, "Post-Slide stun duration. Normal: 20."),
                new OpSlider(postSlideStun, new Vector2(sliderX, firstHeight - labelSpacing), sliderLength)
                {
                    description = "In frames. 1 second = 40 frames. Slugcat will move slower for this time after finishing a slide."
                },
                new OpLabel(labelX, firstHeight - shortSpacing, "Post-Extended Slide stun duration. Normal: 40."),
                new OpSlider(extendedPostSlideStun, new Vector2(sliderX, firstHeight - shortSpacing - labelSpacing), sliderLength)
                {
                    description = "In frames. 1 second = 40 frames. Slugcat will move slower for this time after finishing an extended slide."
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

            secondHeight = firstHeight - shortSpacing * 2 - longSpacing;

            WallPounceOptions = new UIelement[]
            {
                new OpLabel(labelX, firstHeight, "Wallpounce X. Normal: 17, Rivulet: 25."),
                new OpSlider(wallPounceX, new Vector2(sliderX, firstHeight - labelSpacing), sliderLength),
                new OpLabel(labelX, firstHeight-shortSpacing, "Wallpounce Y. Normal: 10, Rivulet: 10."),
                new OpSlider(wallPounceY, new Vector2(sliderX, firstHeight - shortSpacing - labelSpacing), sliderLength),
                new OpLabel(labelX, firstHeight-shortSpacing*2, "Post-Wallpounce stun. Normal: 20, Rivulet: 15."),
                new OpFloatSlider(wallPounceStun, new Vector2(sliderX, firstHeight - shortSpacing*2 - labelSpacing), sliderLength)
                {
                    description = "Time period after a Wallpounce in which slugcat won't be able to drift in the air."
                },
                defaultWallpounce
            };

            RollOptions = new UIelement[]
            {
                new OpLabel(labelX, firstHeight, "Rolling duration. Normal: 15, Gourmand: Infinite*"),
                new OpSlider(rollCount, new Vector2(sliderX, firstHeight - labelSpacing), sliderLength)
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

            jumpTab.AddItems(JumpOptions);
            wallPounceTab.AddItems(WallPounceOptions);
            slideTab.AddItems(SlideOptions);
            slideTab2.AddItems(SlideOptions2);
            rollTab.AddItems(RollOptions);
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

        public override void Update()
        {

            //Have to be replaced with hooks to OnClick; currently bugged. functionality.
            //Also would maybe replace these with a single all defaults button?
            if (defaultJumps._held)
            {
                SetDefaultJumps();
            }
            if (defaultSlide._held)
            {
                SetDefaultSlide();
            }
            if (defaultWallpounce._held)
            {
                SetDefaultWallpounce();
            }
            if (defaultRoll._held)
            {
                SetDefaultRoll();
            }

        }
    }
}