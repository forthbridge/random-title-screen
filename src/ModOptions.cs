using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RandomTitleScreen;

public sealed class ModOptions : OptionsTemplate
{
    public static ModOptions Instance { get; } = new();
    public static void RegisterOI()
    {
        if (MachineConnector.GetRegisteredOI(Plugin.MOD_ID) != Instance)
        {
            MachineConnector.SetRegisteredOI(Plugin.MOD_ID, Instance);
        }
    }


    public static readonly Color WarnRed = new(0.85f, 0.35f, 0.4f);

    #region Options

    public static Configurable<bool> randomizeTitle = Instance.config.Bind("randomizeTite", true, new ConfigurableInfo(
        "Whether the title screen illustrations are randomized",
        null, "", "Randomize Title Screen?"));

    public static Configurable<bool> randomizeOptions = Instance.config.Bind("randomizeOptions", true, new ConfigurableInfo(
        "Whether the ModOptions menu illustrations are randomized.",
        null, "", "Randomize ModOptions Menu?"));

    public static Configurable<bool> unlockAllBackgrounds = Instance.config.Bind("unlockAllBackgrounds", true, new ConfigurableInfo(
        "Whether all backgrounds under ModOptions are unlocked immediately. Overriden by randomizer, disable ModOptions above to use a specific background." +
        "\nNot permanent, will return to normal locks if this setting is disabled.",
        null, "", "Unlock All Backgrounds?"));

    public static Configurable<bool> disableAutoRegionBackground = Instance.config.Bind("disableAutoRegionBackground", true, new ConfigurableInfo(
        "Disables the background art being automatically set to the current region on game start.",
        null, "", "Disable Auto Region\nBackground?"));

    public static Configurable<bool> showCurrentSceneID = Instance.config.Bind("showCurrentSceneID", false, new ConfigurableInfo(
        "Shows the currently displayed menu scene's ID in the top left corner.",
        null, "", "Show Current SceneID?"));

    private OpSimpleButton? selectAllButton;
    private OpSimpleButton? deselectAllButton;

    #endregion


    public ModOptions() => OnConfigChanged += UpdateMenuScenes;


    public const int TAB_COUNT = 2;

    public override void Initialize()
    {
        base.Initialize();

        Tabs = new OpTab[TAB_COUNT];
        int tabIndex = -1;

        InitGeneral(ref tabIndex);
        InitIllustrations(ref tabIndex);
    }

    private void InitGeneral(ref int tabIndex)
    {
        AddTab(ref tabIndex, "General");

        AddCheckBox(randomizeTitle, (string)randomizeTitle.info.Tags[0]);
        AddCheckBox(randomizeOptions, (string)randomizeOptions.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[tabIndex]);

        AddCheckBox(unlockAllBackgrounds, (string)unlockAllBackgrounds.info.Tags[0]);
        AddCheckBox(disableAutoRegionBackground, (string)disableAutoRegionBackground.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[tabIndex]);

        AddCheckBox(showCurrentSceneID, (string)showCurrentSceneID.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[tabIndex]);

        AddNewLine(12);
        DrawBox(ref Tabs[tabIndex]);
    }

    private void InitIllustrations(ref int tabIndex)
    {
        AddTab(ref tabIndex, "Illustrations");
        AddNewLine(2);


        selectAllButton = new OpSimpleButton(new Vector2(350.0f, Pos.y), new Vector2(150.0f, 30.0f), "Select All")
        {
            colorEdge = new Color(1f, 1f, 1f, 1f),
            colorFill = new Color(0.0f, 0.5f, 0.0f, 0.5f),
            description = "Enables all the menu illustrations"
        };
        selectAllButton.OnClick += SelectAllButton_OnClick;
        Tabs[tabIndex].AddItems(selectAllButton);


        deselectAllButton = new OpSimpleButton(new Vector2(100.0f, Pos.y), new Vector2(150.0f, 30.0f), "Deselect All")
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
        CheckBoxes = new OpCheckBox[AvailableMenuScenes.Count];
        ScrollBox = new OpScrollBox(new Vector2(30.0f, 40.0f), new Vector2(540.0f, 380.0f), AvailableMenuScenes.Count * 40.0f + 40.0f, false, false, true);
        Tabs[tabIndex].AddItems(new UIelement[] { ScrollBox });

        for (int i = 0; i < AvailableMenuScenes.Count; i++)
        {
            CheckBoxes[i] = new OpCheckBox(MenuScenesConfig[i], new Vector2(90.0f, GetCheckboxYOffset(i) + 3.0f));

            if (i > 0)
                UIfocusable.MutualVerticalFocusableBind(CheckBoxes[i], CheckBoxes[i - 1]);



            ScrollBox.AddItems(new UIelement[] { CheckBoxes[i] });

            ScrollBox.AddItems(new UIelement[]
            {
                new OpLabel(new Vector2(124.0f, GetCheckboxYOffset(i)), new Vector2(160f, 30f), AvailableMenuScenes[i].value, FLabelAlignment.Left, false, null)
                {
                    bumpBehav = CheckBoxes[i].bumpBehav
                }
            });

        }
    }

    private void SelectAllButton_OnClick(UIfocusable trigger)
    {
        foreach (OpCheckBox checkBox in CheckBoxes)
        {
            checkBox.SetValueBool(true);
        }
    }

    private void DeselectAllButton_OnClick(UIfocusable trigger)
    {
        foreach (OpCheckBox checkBox in CheckBoxes)
        {
            checkBox.SetValueBool(false);
        }
    }

    

    private static float GetCheckboxYOffset(int index) => (AvailableMenuScenes.Count - index) * 40f - 15.01f;

    private static OpCheckBox[] CheckBoxes = null!;
    private static OpScrollBox ScrollBox = null!;


    private static string GetSceneKey(Menu.MenuScene.SceneID sceneID) => "MenuScene_" + sceneID.value;

    private static List<Menu.MenuScene.SceneID> AvailableMenuScenes { get; } = new();
    public static List<Menu.MenuScene.SceneID> EnabledMenuScenes { get; } = new();
    
    private static List<Configurable<bool>> MenuScenesConfig { get; } = new();


    public static void UpdateMenuScenes()
    {
        AvailableMenuScenes.Clear();

        foreach (var enumName in Menu.MenuScene.SceneID.values.entries)
        {
            var sceneID = new Menu.MenuScene.SceneID(enumName);

            // Literally crashes the game
            if (sceneID == Menu.MenuScene.SceneID.Outro_3_Face) continue;

            // Looks strange
            if (sceneID == Menu.MenuScene.SceneID.Empty) continue;

            if (sceneID == Menu.MenuScene.SceneID.Intro_14_Title) continue;

            if (sceneID == Menu.MenuScene.SceneID.Intro_9_Rainy_Climb) continue;

            if (sceneID == Menu.MenuScene.SceneID.Intro_10_Fall) continue;


            AvailableMenuScenes.Add(sceneID);
        }


        EnabledMenuScenes.Clear();

        foreach (var sceneID in AvailableMenuScenes)
        {
            var sceneKey = GetSceneKey(sceneID);

            if (!MenuScenesConfig.Any(x => x.key == sceneKey))
            {
                MenuScenesConfig.Add(Instance.config.Bind(GetSceneKey(sceneID), IsSceneDefaultEnabled(sceneID)));
            }

            var thisSceneCheckbox = MenuScenesConfig.FirstOrDefault(x => x.key == sceneKey);

            if (thisSceneCheckbox != null && thisSceneCheckbox.Value)
            {
                EnabledMenuScenes.Add(sceneID);
            }
        }

        // Plugin.Logger.LogInfo($"Updated available ({AvailableMenuScenes}) & enabled ({EnabledMenuScenes.Count}) menu scenes");
    }

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
}