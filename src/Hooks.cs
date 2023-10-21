using IL.Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RandomTitleScreen;

public static class Hooks
{
    public static void ApplyInit()
    {
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        On.RainWorld.PostModsInit += RainWorld_PostModsInit;
    }


    private static bool isInit = false;

    private static void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        try
        {
            if (isInit) return;
            isInit = true;

            var mod = ModManager.ActiveMods.FirstOrDefault(mod => mod.id == Plugin.MOD_ID);

            Plugin.MOD_NAME = mod.name;
            Plugin.VERSION = mod.version;
            Plugin.AUTHORS = mod.authors;

            ApplyHooks();
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError("OnModsInit:\n" + ex);
        }
        finally
        {
            orig(self);
        }
    }
    
    private static void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        try
        {
            ModOptions.RegisterOI();
            ModOptions.UpdateMenuScenes();
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError("PostModsInit:\n" + ex);
        }
        finally
        {
            orig(self);
        }
    }



    private static void ApplyHooks()
    {
        On.MoreSlugcats.BackgroundOptionsMenu.IndexUnlocked += BackgroundOptionsMenu_IndexUnlocked;
        On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch_ProcessID;

        On.Menu.MainMenu.BackgroundScene += MainMenu_BackgroundScene;
        On.Menu.MainMenu.ctor += MainMenu_ctor;

        MainMenu.BackgroundScene += MainMenu_BackgroundSceneIL;

        OptionsMenu.ctor += OptionsMenu_ctorIL;
        ModdingMenu.ctor += ModdingMenu_ctorIL;

        InputOptionsMenu.ctor += InputOptionsMenu_ctorIL;
        MultiplayerMenu.ctor += MultiplayerMenu_ctorIL;

        On.Menu.Menu.Update += Menu_Update;
        On.MainLoopProcess.ShutDownProcess += MainLoopProcess_ShutDownProcess;
    }

    // Randomize background
    private static void ProcessManager_RequestMainProcessSwitch_ProcessID(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager self, ProcessManager.ProcessID ID)
    {
        orig(self, ID);

        if (ModOptions.EnabledMenuScenes == null || ModOptions.EnabledMenuScenes.Count == 0) return;

        var scene = ModOptions.EnabledMenuScenes[Random.Range(0, ModOptions.EnabledMenuScenes.Count)];

        if (ModOptions.randomizeTitle.Value)
        {
            self.rainWorld.options.titleBackground = scene;
        }

        if (ModOptions.randomizeOptions.Value)
        {
            self.rainWorld.options.subBackground = scene;
        }
    }

    // Unlock all backgrounds
    private static bool BackgroundOptionsMenu_IndexUnlocked(On.MoreSlugcats.BackgroundOptionsMenu.orig_IndexUnlocked orig, MoreSlugcats.BackgroundOptionsMenu self, int ind, List<string> regions)
    {
        if (ModOptions.unlockAllBackgrounds.Value) return true;

        return orig(self, ind, regions);
    }



    // Most of these are necessary to display the custom backgrounds without Remix enabled
    private static void MultiplayerMenu_ctorIL(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        c.GotoNext(MoveType.After,
            x => x.MatchLdsfld<ModManager>(nameof(ModManager.MMF)));

        c.Emit(OpCodes.Ldc_I4_1);
        c.Emit(OpCodes.Or);
    }

    private static void InputOptionsMenu_ctorIL(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        c.GotoNext(MoveType.After,
            x => x.MatchLdsfld<ModManager>(nameof(ModManager.MMF)));

        c.Emit(OpCodes.Ldc_I4_1);
        c.Emit(OpCodes.Or);
    }

    private static void ModdingMenu_ctorIL(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        c.GotoNext(MoveType.After,
            x => x.MatchLdsfld<ModManager>(nameof(ModManager.MMF)));

        c.Emit(OpCodes.Ldc_I4_1);
        c.Emit(OpCodes.Or);
    }

    private static void OptionsMenu_ctorIL(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        c.GotoNext(MoveType.After,
            x => x.MatchLdsfld<ModManager>(nameof(ModManager.MMF)));

        c.Emit(OpCodes.Ldc_I4_1);
        c.Emit(OpCodes.Or);
    }

    private static void MainMenu_BackgroundSceneIL(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        while (c.TryGotoNext(MoveType.Before,
            x => x.MatchLdsfld<ModManager>(nameof(ModManager.MMF)),
            x => x.MatchBrfalse(out _)
            ))
        {
            c.Index++;
            c.Emit(OpCodes.Ldc_I4_1);
            c.Emit(OpCodes.Or);

            c.Index++;
        };
    }


    // IL Hook didn't work, hacky workaround to show custom background
    private static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg) => orig(self, manager, true);

    private static Menu.MenuScene.SceneID MainMenu_BackgroundScene(On.Menu.MainMenu.orig_BackgroundScene orig, Menu.MainMenu self)
    {
        if (ModOptions.disableAutoRegionBackground.Value)
        {
            return self.manager.rainWorld.options.TitleBackground;
        }

        return orig(self);
    }


    private static FLabel? MenuSceneIDLabel;

    private static void Menu_Update(On.Menu.Menu.orig_Update orig, Menu.Menu self)
    {
        orig(self);

        if (!ModOptions.showCurrentSceneID.Value) return;

        if (MenuSceneIDLabel == null)
        {
            MenuSceneIDLabel = new(Custom.GetFont(), "");

            Futile.stage.AddChild(MenuSceneIDLabel);

            MenuSceneIDLabel.x = Custom.rainWorld.options.ScreenSize.x / 2f + 0.01f;
            MenuSceneIDLabel.y = 755.01f;

            MenuSceneIDLabel.color = Color.white;
        }

        MenuSceneIDLabel.isVisible = ModOptions.showCurrentSceneID.Value;
        MenuSceneIDLabel.text = "Current SceneID: " + (self.scene?.sceneID?.value ?? "unknown");
    }

    private static void MainLoopProcess_ShutDownProcess(On.MainLoopProcess.orig_ShutDownProcess orig, MainLoopProcess self)
    {
        orig(self);

        if (MenuSceneIDLabel != null)
        {
            MenuSceneIDLabel.RemoveFromContainer();
            MenuSceneIDLabel = null;
        }
    }

}
