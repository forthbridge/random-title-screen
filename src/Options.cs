using System;
using System.Collections.Generic;
using System.IO;
using IL.Menu;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using UnityEngine;

namespace RandomTitleScreen
{
    // Based on the options script from SBCameraScroll by SchuhBaum
    // https://github.com/SchuhBaum/SBCameraScroll/blob/Rain-World-v1.9/SourceCode/MainModOptions.cs
    public class Options : OptionInterface
    {
        public static Options instance = new Options();
        private const string AUTHORS_NAME = "forthbridge";

        #region Options

        public static Configurable<bool> randomizeTitle = instance.config.Bind("randomizeTite", true, new ConfigurableInfo(
            "Whether the title screen illustrations are randomized",
            null, "", "Randomize Title Screen?"));

        public static Configurable<bool> randomizeOptions = instance.config.Bind("randomizeOptions", true, new ConfigurableInfo(
            "Whether the options menu illustrations are randomized.",
            null, "", "Randomize Options Menu?"));

        public static Configurable<bool> unlockAllBackgrounds = instance.config.Bind("unlockAllBackgrounds", true, new ConfigurableInfo(
            "Whether all backgrounds under options are unlocked immediately. Overriden by randomizer, disable options above to use a specific background." +
            "\nNot permanent, will return to normal locks if this setting is disabled.",
            null, "", "Unlock All Backgrounds?"));

        public static Configurable<bool> disableAutoRegionBackground = instance.config.Bind("disableAutoRegionBackground", true, new ConfigurableInfo(
            "Disables the background art being automatically set to the current region on game start.",
            null, "", "Disable Auto Region\nBackground?"));

        private OpSimpleButton? selectAllButton;

        private OpSimpleButton? deselectAllButton;

        #endregion

        #region Parameters

        private readonly float spacing = 20f;
        private readonly float fontHeight = 20f;
        private readonly int numberOfCheckboxes = 2;
        private readonly float checkBoxSize = 60.0f;
        private float CheckBoxWithSpacing => checkBoxSize + 0.25f * spacing;

        private Vector2 marginX = new();
        private Vector2 pos = new();

        private readonly List<float> boxEndPositions = new();

        private readonly List<Configurable<bool>> checkBoxConfigurables = new();
        private readonly List<OpLabel> checkBoxesTextLabels = new();

        private readonly List<Configurable<string>> comboBoxConfigurables = new();
        private readonly List<List<ListItem>> comboBoxLists = new();
        private readonly List<bool> comboBoxAllowEmpty = new();
        private readonly List<OpLabel> comboBoxesTextLabels = new();

        private readonly List<Configurable<int>> sliderConfigurables = new();
        private readonly List<string> sliderMainTextLabels = new();
        private readonly List<OpLabel> sliderTextLabelsLeft = new();
        private readonly List<OpLabel> sliderTextLabelsRight = new();

        private readonly List<OpLabel> textLabels = new();

        #endregion

        public Options() => OnConfigChanged += UpdateAvailableScenes;

        private const int NUMBER_OF_TABS = 2;

        public override void Initialize()
        {
            base.Initialize();

            Tabs = new OpTab[NUMBER_OF_TABS];
            int tabIndex = -1;

            AddTab(ref tabIndex, "General");

            AddCheckBox(randomizeTitle, (string)randomizeTitle.info.Tags[0]);
            AddCheckBox(randomizeOptions, (string)randomizeOptions.info.Tags[0]);
            DrawCheckBoxes(ref Tabs[tabIndex]);

            AddCheckBox(unlockAllBackgrounds, (string)unlockAllBackgrounds.info.Tags[0]);
            AddCheckBox(disableAutoRegionBackground, (string)disableAutoRegionBackground.info.Tags[0]);
            DrawCheckBoxes(ref Tabs[tabIndex]);

            AddNewLine(15);
            DrawBox(ref Tabs[tabIndex]);



            AddTab(ref tabIndex, "Illustrations");
            AddNewLine(2);


            selectAllButton = new OpSimpleButton(new Vector2(350.0f, pos.y), new Vector2(150.0f, 30.0f), "Select All")
            {
                colorEdge = new Color(1f, 1f, 1f, 1f),
                colorFill = new Color(0.0f, 0.5f, 0.0f, 0.5f),
                description = "Enables all the menu illustrations"
            };
            selectAllButton.OnClick += SelectAllButton_OnClick;
            Tabs[tabIndex].AddItems(selectAllButton);


            deselectAllButton = new OpSimpleButton(new Vector2(100.0f, pos.y), new Vector2(150.0f, 30.0f), "Deselect All")
            {
                colorEdge = new Color(1f, 1f, 1f, 1f),
                colorFill = new Color(0.5f, 0.0f, 0.0f, 0.5f),
                description = "Disables all the menu illustrations."
            };
            deselectAllButton.OnClick += DeselectAllButton_OnClick;
            Tabs[tabIndex].AddItems(deselectAllButton);


            DrawScrollbox(ref tabIndex);

            AddNewLine(19);
            DrawBox(ref Tabs[tabIndex]);
        }



        private void DrawScrollbox(ref int tabIndex)
        {
            checkBoxes = new OpCheckBox[allMenuScenes.Count];
            scrollBox = new OpScrollBox(new Vector2(30.0f, 40.0f), new Vector2(540.0f, 380.0f), allMenuScenes.Count * 40.0f + 40.0f, false, false, true);
            Tabs[tabIndex].AddItems(new UIelement[] { scrollBox });

            for (int i = 0; i < allMenuScenes.Count; i++)
            {
                checkBoxes[i] = new OpCheckBox(menuScenesConfig[i], new Vector2(90.0f, GetCheckboxYOffset(i) + 3.0f));

                if (i > 0)
                    UIfocusable.MutualVerticalFocusableBind(checkBoxes[i], checkBoxes[i - 1]);



                scrollBox.AddItems(new UIelement[] { checkBoxes[i] });

                scrollBox.AddItems(new UIelement[]
                {
                    new OpLabel(new Vector2(124.0f, GetCheckboxYOffset(i)), new Vector2(160f, 30f), allMenuScenes[i].value, FLabelAlignment.Left, false, null)
                    {
                        bumpBehav = checkBoxes[i].bumpBehav
                    }
                });

            }
        }

        private void SelectAllButton_OnClick(UIfocusable trigger)
        {
            foreach (OpCheckBox checkBox in checkBoxes)
                checkBox.SetValueBool(true);
        }

        private void DeselectAllButton_OnClick(UIfocusable trigger)
        {
            foreach (OpCheckBox checkBox in checkBoxes)
                checkBox.SetValueBool(false);
        }



        public static List<Menu.MenuScene.SceneID> enabledMenuScenes = new List<Menu.MenuScene.SceneID>();

        private static readonly List<Menu.MenuScene.SceneID> allMenuScenes = new List<Menu.MenuScene.SceneID>();
        private static readonly List<Configurable<bool>> menuScenesConfig  = new List<Configurable<bool>>();

        private static OpCheckBox[] checkBoxes = null!;
        private static OpScrollBox scrollBox = null!;



        public static void InitializeMenuScenesConfig()
        {
            int sceneCount = 0;

            for (int i = 0; i < Menu.MenuScene.SceneID.values.Count; i++)
            {
                string enumName = Menu.MenuScene.SceneID.values.entries[i];
                Menu.MenuScene.SceneID sceneID = new Menu.MenuScene.SceneID(enumName);



                // Literally crashes the game
                if (sceneID == Menu.MenuScene.SceneID.Outro_3_Face) continue;

                // Looks strange
                if (sceneID == Menu.MenuScene.SceneID.Empty) continue;

                if (sceneID == Menu.MenuScene.SceneID.Intro_14_Title) continue;

                if (sceneID == Menu.MenuScene.SceneID.Intro_9_Rainy_Climb) continue;

                if (sceneID == Menu.MenuScene.SceneID.Intro_10_Fall) continue;



                allMenuScenes.Add(sceneID);
                menuScenesConfig.Add(instance.config.Bind(GenerateSceneKey(allMenuScenes[sceneCount]), IsSceneDefaultEnabled(allMenuScenes[sceneCount])));

                sceneCount++;
            }

            UpdateAvailableScenes();
        }

        private static void UpdateAvailableScenes()
        {
            enabledMenuScenes.Clear();

            for (int i = 0; i < allMenuScenes.Count; i++)
                if (menuScenesConfig[i].Value)
                    enabledMenuScenes.Add(allMenuScenes[i]);

            Plugin.Logger.LogWarning($"Updated enabled scenes! {enabledMenuScenes.Count}");
        }



        private static float GetCheckboxYOffset(int index) => (allMenuScenes.Count - index) * 40f - 15.01f;

        private static string GenerateSceneKey(Menu.MenuScene.SceneID sceneID) => "MenuScene_" + sceneID.value;

        // Exclude the following from the defaults
        private static bool IsSceneDefaultEnabled(Menu.MenuScene.SceneID sceneID)
        {
            // Parallax broken but works
            if (sceneID == Menu.MenuScene.SceneID.Intro_2_Branch) return false;


            // Death and Starve
            if (sceneID == Menu.MenuScene.SceneID.RedsDeathStatisticsBkg) return false;

            if (sceneID == Menu.MenuScene.SceneID.NewDeath) return false;

            if (sceneID == Menu.MenuScene.SceneID.StarveScreen) return false;

            if (sceneID == Menu.MenuScene.SceneID.Slugcat_Dead_Red) return false;


            // Selection Menus
            if (sceneID == Menu.MenuScene.SceneID.Slugcat_Red) return false;

            if (sceneID == Menu.MenuScene.SceneID.Ghost_Red) return false;

            if (sceneID == Menu.MenuScene.SceneID.Slugcat_White) return false;

            if (sceneID == Menu.MenuScene.SceneID.Ghost_White) return false;

            if (sceneID == Menu.MenuScene.SceneID.Slugcat_Yellow) return false;

            if (sceneID == Menu.MenuScene.SceneID.Ghost_Yellow) return false;


            // Downpour Selection Menus
            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.AltEnd_Survivor) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.AltEnd_Monk) return false;


            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Gourmand) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.End_Gourmand) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.AltEnd_Gourmand) return false;
            
            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.AltEnd_Gourmand_Full) return false;



            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Artificer) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Artificer_Robo) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Artificer_Robo2) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.End_Artificer) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.AltEnd_Artificer_Portrait) return false;


            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Rivulet) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Rivulet_Cell) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.End_Rivulet) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.AltEnd_Rivulet) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.AltEnd_Rivulet_Robe) return false;


            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Spear) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.End_Spear) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.AltEnd_Spearmaster) return false;


            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Saint) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Saint_Max) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.End_Saint) return false;


            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Inv) return false;

            if (sceneID == MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.End_Inv) return false;

            return true;
        }



        #region UI Elements

        private void AddTab(ref int tabIndex, string tabName)
        {
            tabIndex++;
            Tabs[tabIndex] = new OpTab(this, tabName);
            InitializeMarginAndPos();

            AddNewLine();
            AddTextLabel(Plugin.MOD_NAME, bigText: true);
            DrawTextLabels(ref Tabs[tabIndex]);

            AddNewLine(0.5f);
            AddTextLabel("Version " + Plugin.VERSION, FLabelAlignment.Left);
            AddTextLabel("by " + AUTHORS_NAME, FLabelAlignment.Right);
            DrawTextLabels(ref Tabs[tabIndex]);

            AddNewLine();
            AddBox();
        }

        private void InitializeMarginAndPos()
        {
            marginX = new Vector2(50f, 550f);
            pos = new Vector2(50f, 600f);
        }

        private void AddNewLine(float spacingModifier = 1f)
        {
            pos.x = marginX.x; // left margin
            pos.y -= spacingModifier * spacing;
        }

        private void AddBox()
        {
            marginX += new Vector2(spacing, -spacing);
            boxEndPositions.Add(pos.y); // end position > start position
            AddNewLine();
        }

        private void DrawBox(ref OpTab tab)
        {
            marginX += new Vector2(-spacing, spacing);
            AddNewLine();

            float boxWidth = marginX.y - marginX.x;
            int lastIndex = boxEndPositions.Count - 1;

            tab.AddItems(new OpRect(pos, new Vector2(boxWidth, boxEndPositions[lastIndex] - pos.y)));
            boxEndPositions.RemoveAt(lastIndex);
        }

        private void AddCheckBox(Configurable<bool> configurable, string text)
        {
            checkBoxConfigurables.Add(configurable);
            checkBoxesTextLabels.Add(new OpLabel(new Vector2(), new Vector2(), text, FLabelAlignment.Left));
        }

        private void DrawCheckBoxes(ref OpTab tab) // changes pos.y but not pos.x
        {
            if (checkBoxConfigurables.Count != checkBoxesTextLabels.Count) return;

            float width = marginX.y - marginX.x;
            float elementWidth = (width - (numberOfCheckboxes - 1) * 0.5f * spacing) / numberOfCheckboxes;
            pos.y -= checkBoxSize;
            float _posX = pos.x;

            for (int checkBoxIndex = 0; checkBoxIndex < checkBoxConfigurables.Count; ++checkBoxIndex)
            {
                Configurable<bool> configurable = checkBoxConfigurables[checkBoxIndex];
                OpCheckBox checkBox = new(configurable, new Vector2(_posX, pos.y))
                {
                    description = configurable.info?.description ?? ""
                };
                tab.AddItems(checkBox);
                _posX += CheckBoxWithSpacing;

                OpLabel checkBoxLabel = checkBoxesTextLabels[checkBoxIndex];
                checkBoxLabel.pos = new Vector2(_posX, pos.y + 2f);
                checkBoxLabel.size = new Vector2(elementWidth - CheckBoxWithSpacing, fontHeight);
                tab.AddItems(checkBoxLabel);

                if (checkBoxIndex < checkBoxConfigurables.Count - 1)
                {
                    if ((checkBoxIndex + 1) % numberOfCheckboxes == 0)
                    {
                        AddNewLine();
                        pos.y -= checkBoxSize;
                        _posX = pos.x;
                    }
                    else
                    {
                        _posX += elementWidth - CheckBoxWithSpacing + 0.5f * spacing;
                    }
                }
            }

            checkBoxConfigurables.Clear();
            checkBoxesTextLabels.Clear();
        }

        private void AddTextLabel(string text, FLabelAlignment alignment = FLabelAlignment.Center, bool bigText = false)
        {
            float textHeight = (bigText ? 2f : 1f) * fontHeight;
            if (textLabels.Count == 0)
            {
                pos.y -= textHeight;
            }

            OpLabel textLabel = new(new Vector2(), new Vector2(20f, textHeight), text, alignment, bigText) // minimal size.x = 20f
            {
                autoWrap = true
            };
            textLabels.Add(textLabel);
        }

        private void DrawTextLabels(ref OpTab tab)
        {
            if (textLabels.Count == 0)
            {
                return;
            }

            float width = (marginX.y - marginX.x) / textLabels.Count;
            foreach (OpLabel textLabel in textLabels)
            {
                textLabel.pos = pos;
                textLabel.size += new Vector2(width - 20f, 0.0f);
                tab.AddItems(textLabel);
                pos.x += width;
            }

            pos.x = marginX.x;
            textLabels.Clear();
        }

        #endregion
    }
}