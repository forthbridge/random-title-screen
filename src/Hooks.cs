using IL.Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Media;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RandomTitleScreen
{
    internal static class Hooks
    {
        public static void ApplyHooks()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;

            On.MoreSlugcats.BackgroundOptionsMenu.IndexUnlocked += BackgroundOptionsMenu_IndexUnlocked;

            On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch_ProcessID;

            On.Menu.MainMenu.BackgroundScene += MainMenu_BackgroundScene;

            On.Menu.MainMenu.ctor += MainMenu_ctor;
        }

        private static bool isInit = false;

        private static void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            if (isInit) return;
            isInit = true;

            MachineConnector.SetRegisteredOI(RandomTitleScreen.MOD_ID, Options.instance);
            Options.InitializeMenuScenesConfig();

            try
            {
                MainMenu.BackgroundScene += MainMenu_BackgroundSceneIL;

                //MainMenu.ctor += MainMenu_ctorIL;

                OptionsMenu.ctor += OptionsMenu_ctorIL;
                ModdingMenu.ctor += ModdingMenu_ctorIL;
                InputOptionsMenu.ctor += InputOptionsMenu_ctorIL;
                MultiplayerMenu.ctor += MultiplayerMenu_ctorIL;
            }
            catch (Exception ex)
            {
                RandomTitleScreen.Logger.LogError(ex);
            }
        }

        // Most of these are necessary to display the custom backgrounds without Remix enabled
        #region IL Hooks

        private static void MultiplayerMenu_ctorIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Disable MMF Check
            c.GotoNext(MoveType.Before,
                x => x.MatchLdsfld<ModManager>("MMF")
                );

            c.Remove();
            c.Emit(OpCodes.Ldc_I4_1);
        }

        private static void InputOptionsMenu_ctorIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Disable MMF Check
            c.GotoNext(MoveType.Before,
                x => x.MatchLdsfld<ModManager>("MMF")
                );

            c.Remove();
            c.Emit(OpCodes.Ldc_I4_1);
        }

        private static void ModdingMenu_ctorIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Disable MMF Check
            c.GotoNext(MoveType.Before,
                x => x.MatchLdsfld<ModManager>("MMF")
                );

            c.Remove();
            c.Emit(OpCodes.Ldc_I4_1);
        }

        private static void OptionsMenu_ctorIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Disable MMF Check
            c.GotoNext(MoveType.Before,
                x => x.MatchLdsfld<ModManager>("MMF")
                );

            c.Remove();
            c.Emit(OpCodes.Ldc_I4_1);
        }

        // Override region backgrounds and force MMF's custom backgrounds
        private static void MainMenu_BackgroundSceneIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Disable MMF Check
            while (c.TryGotoNext(MoveType.Before,
                x => x.MatchLdsfld<ModManager>("MMF"),
                x => x.MatchBrfalse(out _)
                ))
            {
                c.Remove();
                c.Emit(OpCodes.Ldc_I4_1);
            };
        }

        // For whatever reason this IL Hook refuses to work???
        private static void MainMenu_ctorIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Disable MMF Check
            c.GotoNext(MoveType.Before,
                x => x.MatchLdsfld<ModManager>("MMF"),
                x => x.MatchBrfalse(out _)
                );

            c.Remove();
            c.Emit(OpCodes.Ldc_I4_1);
        }
        #endregion

        // IL Hook didn't work, hacky workaround to show custom background
        private static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg) => orig(self, manager, true);

        private static Menu.MenuScene.SceneID MainMenu_BackgroundScene(On.Menu.MainMenu.orig_BackgroundScene orig, Menu.MainMenu self)
        {
            if (Options.disableAutoRegionBackground.Value) return self.manager.rainWorld.options.TitleBackground;

            return orig(self);
        }

        // Randomize background
        private static void ProcessManager_RequestMainProcessSwitch_ProcessID(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            orig(self, ID);

            if (Options.enabledMenuScenes == null || Options.enabledMenuScenes.Count == 0) return;

            if (Options.randomizeTitle.Value)
            {
                self.rainWorld.options.titleBackground = Options.enabledMenuScenes[Random.Range(0, Options.enabledMenuScenes.Count)];
            }

            if (Options.randomizeOptions.Value)
            {
                self.rainWorld.options.subBackground = Options.enabledMenuScenes[Random.Range(0, Options.enabledMenuScenes.Count)];
            }

            RandomTitleScreen.Logger.LogWarning($"Title Background: {self.rainWorld.options.titleBackground}");
            RandomTitleScreen.Logger.LogWarning($"Sub Background: {self.rainWorld.options.subBackground}");
        }

        // Unlock all backgrounds
        private static bool BackgroundOptionsMenu_IndexUnlocked(On.MoreSlugcats.BackgroundOptionsMenu.orig_IndexUnlocked orig, MoreSlugcats.BackgroundOptionsMenu self, int ind, List<string> regions)
        {
            if (Options.unlockAllBackgrounds.Value) return true;

            return orig(self, ind, regions);
        }

    }
}
