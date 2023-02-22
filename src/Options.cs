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

        public static Configurable<bool> unlockAllBackgrounds = instance.config.Bind("unlockAllBackgrounds", false, new ConfigurableInfo(
            "Whether all backgrounds under options are unlocked immediately. Overriden by randomizer, disable options above to use a specific background." +
            "\nNot permanent, will return to normal locks if this setting is disabled.",
            null, "", "Unlock All Backgrounds?"));

        private OpSimpleButton? selectAllButton;

        private OpSimpleButton? deselectAllButton;
        #endregion

        #region Parameters
        private readonly float spacing = 20f;
        private readonly float fontHeight = 20f;
        private readonly int numberOfCheckboxes = 2;
        private readonly float checkBoxSize = 60.0f;
        private float CheckBoxWithSpacing => checkBoxSize + 0.25f * spacing;
        #endregion

        #region Variables
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

        public Options()
        {
            OnConfigChanged += UpdateAvailableScenes;
        }

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
            checkBoxes = new OpCheckBox[allMenuScenes.Length];
            scrollBox = new OpScrollBox(new Vector2(30.0f, 40.0f), new Vector2(540.0f, 380.0f), allMenuScenes.Length * 40.0f + 40.0f, false, false, true);
            Tabs[tabIndex].AddItems(new UIelement[] { scrollBox });

            for (int i = 0; i < allMenuScenes.Length; i++)
            {
                checkBoxes[i] = new OpCheckBox(menuScenesConfig[i], new Vector2(90.0f, GetCheckboxYOffset(i) + 3.0f));

                if (i > 0)
                {
                    UIfocusable.MutualVerticalFocusableBind(checkBoxes[i], checkBoxes[i - 1]);
                }

                scrollBox.AddItems(new UIelement[] { checkBoxes[i] });

                scrollBox.AddItems(new UIelement[]
                {
                    new OpLabel(new Vector2(124.0f, GetCheckboxYOffset(i)), new Vector2(160f, 30f), allMenuScenes[i].ToString(), FLabelAlignment.Left, false, null)
                    {
                        bumpBehav = checkBoxes[i].bumpBehav
                    }
                });

            }
        }

        private void SelectAllButton_OnClick(UIfocusable trigger)
        {
            foreach (OpCheckBox checkBox in checkBoxes)
            {
                checkBox.SetValueBool(true);
            }
        }

        private void DeselectAllButton_OnClick(UIfocusable trigger)
        {
            foreach (OpCheckBox checkBox in checkBoxes)
            {
                checkBox.SetValueBool(false);
            }
        }


        public static List<Menu.MenuScene.SceneID> enabledMenuScenes = new List<Menu.MenuScene.SceneID>();
        private static Configurable<bool>[] menuScenesConfig = null!;

        private static Menu.MenuScene.SceneID[] allMenuScenes = null!;

        private static OpCheckBox[] checkBoxes = null!;
        private static OpScrollBox scrollBox = null!;

        public static void InitializeMenuScenesConfig()
        {
            allMenuScenes = new Menu.MenuScene.SceneID[Menu.MenuScene.SceneID.values.Count];
            menuScenesConfig = new Configurable<bool>[allMenuScenes.Length];

            for (int i = 0; i < allMenuScenes.Length; i++)
            {
                string enumName = Menu.MenuScene.SceneID.values.entries[i];
                Menu.MenuScene.SceneID sceneID = new Menu.MenuScene.SceneID(enumName);

                allMenuScenes[i] = sceneID;

                menuScenesConfig[i] = instance.config.Bind(GenerateSceneKey(allMenuScenes[i]), IsSceneDefaultEnabled(allMenuScenes[i]));
            }

            UpdateAvailableScenes();
        }

        private static void UpdateAvailableScenes()
        {
            enabledMenuScenes.Clear();

            for (int i = 0; i < allMenuScenes.Length; i++)
            {
                if (menuScenesConfig[i].Value)
                {
                    enabledMenuScenes.Add(allMenuScenes[i]);
                }
            }

            RandomTitleScreen.Logger.LogWarning("Updated enabled scenes!");
        }

        private static float GetCheckboxYOffset(int index) => (allMenuScenes.Length - index) * 40f - 15.01f;

        private static string GenerateSceneKey(Menu.MenuScene.SceneID sceneID) => "MenuScene_" + sceneID.value;

        private static bool IsSceneDefaultEnabled(Menu.MenuScene.SceneID sceneID)
        {
            if (sceneID == Menu.MenuScene.SceneID.Empty) return false;

            return true;
        }

        #region UI Elements
        private void AddTab(ref int tabIndex, string tabName)
        {
            tabIndex++;
            Tabs[tabIndex] = new OpTab(this, tabName);
            InitializeMarginAndPos();

            AddNewLine();
            AddTextLabel(RandomTitleScreen.MOD_NAME, bigText: true);
            DrawTextLabels(ref Tabs[tabIndex]);

            AddNewLine(0.5f);
            AddTextLabel("Version " + RandomTitleScreen.VERSION, FLabelAlignment.Left);
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

        private void AddComboBox(Configurable<string> configurable, List<ListItem> list, string text, bool allowEmpty = false)
        {
            OpLabel opLabel = new(new Vector2(), new Vector2(0.0f, fontHeight), text, FLabelAlignment.Center, false);
            comboBoxesTextLabels.Add(opLabel);
            comboBoxConfigurables.Add(configurable);
            comboBoxLists.Add(list);
            comboBoxAllowEmpty.Add(allowEmpty);
        }

        private void DrawComboBoxes(ref OpTab tab)
        {
            if (comboBoxConfigurables.Count != comboBoxesTextLabels.Count) return;
            if (comboBoxConfigurables.Count != comboBoxLists.Count) return;
            if (comboBoxConfigurables.Count != comboBoxAllowEmpty.Count) return;

            float offsetX = (marginX.y - marginX.x) * 0.1f;
            float width = (marginX.y - marginX.x) * 0.4f;

            for (int comboBoxIndex = 0; comboBoxIndex < comboBoxConfigurables.Count; ++comboBoxIndex)
            {
                AddNewLine(1.25f);
                pos.x += offsetX;

                OpLabel opLabel = comboBoxesTextLabels[comboBoxIndex];
                opLabel.pos = pos;
                opLabel.size += new Vector2(width, 2f); // size.y is already set
                pos.x += width;

                Configurable<string> configurable = comboBoxConfigurables[comboBoxIndex];
                OpComboBox comboBox = new(configurable, pos, width, comboBoxLists[comboBoxIndex])
                {
                    allowEmpty = comboBoxAllowEmpty[comboBoxIndex],
                    description = configurable.info?.description ?? ""
                };
                tab.AddItems(opLabel, comboBox);

                // don't add a new line on the last element
                if (comboBoxIndex < comboBoxConfigurables.Count - 1)
                {
                    AddNewLine();
                    pos.x = marginX.x;
                }
            }

            comboBoxesTextLabels.Clear();
            comboBoxConfigurables.Clear();
            comboBoxLists.Clear();
            comboBoxAllowEmpty.Clear();
        }

        private void AddSlider(Configurable<int> configurable, string text, string sliderTextLeft = "", string sliderTextRight = "")
        {
            sliderConfigurables.Add(configurable);
            sliderMainTextLabels.Add(text);
            sliderTextLabelsLeft.Add(new OpLabel(new Vector2(), new Vector2(), sliderTextLeft, alignment: FLabelAlignment.Right)); // set pos and size when drawing
            sliderTextLabelsRight.Add(new OpLabel(new Vector2(), new Vector2(), sliderTextRight, alignment: FLabelAlignment.Left));
        }

        private void DrawSliders(ref OpTab tab)
        {
            if (sliderConfigurables.Count != sliderMainTextLabels.Count) return;
            if (sliderConfigurables.Count != sliderTextLabelsLeft.Count) return;
            if (sliderConfigurables.Count != sliderTextLabelsRight.Count) return;

            float width = marginX.y - marginX.x;
            float sliderCenter = marginX.x + 0.5f * width;
            float sliderLabelSizeX = 0.2f * width;
            float sliderSizeX = width - 2f * sliderLabelSizeX - spacing;

            for (int sliderIndex = 0; sliderIndex < sliderConfigurables.Count; ++sliderIndex)
            {
                AddNewLine(2f);

                OpLabel opLabel = sliderTextLabelsLeft[sliderIndex];
                opLabel.pos = new Vector2(marginX.x, pos.y + 5f);
                opLabel.size = new Vector2(sliderLabelSizeX, fontHeight);
                tab.AddItems(opLabel);

                Configurable<int> configurable = sliderConfigurables[sliderIndex];
                OpSlider slider = new(configurable, new Vector2(sliderCenter - 0.5f * sliderSizeX, pos.y), (int)sliderSizeX)
                {
                    size = new Vector2(sliderSizeX, fontHeight),
                    description = configurable.info?.description ?? ""
                };
                tab.AddItems(slider);

                opLabel = sliderTextLabelsRight[sliderIndex];
                opLabel.pos = new Vector2(sliderCenter + 0.5f * sliderSizeX + 0.5f * spacing, pos.y + 5f);
                opLabel.size = new Vector2(sliderLabelSizeX, fontHeight);
                tab.AddItems(opLabel);

                AddTextLabel(sliderMainTextLabels[sliderIndex]);
                DrawTextLabels(ref tab);

                if (sliderIndex < sliderConfigurables.Count - 1)
                {
                    AddNewLine();
                }
            }

            sliderConfigurables.Clear();
            sliderMainTextLabels.Clear();
            sliderTextLabelsLeft.Clear();
            sliderTextLabelsRight.Clear();
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