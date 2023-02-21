using System.Collections.Generic;
using System.Media;
using UnityEngine;

namespace RandomTitleScreen
{
    internal static class Hooks
    {
        public static void ApplyHooks()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;

            On.MoreSlugcats.BackgroundOptionsMenu.IndexUnlocked += BackgroundOptionsMenu_IndexUnlocked;

            On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch_ProcessID;
        }

        private static void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);


            if (isInit) return;
            isInit = true;

            MachineConnector.SetRegisteredOI(RandomTitleScreen.MOD_ID, Options.instance);
        }

        // Randomize background
        private static void ProcessManager_RequestMainProcessSwitch_ProcessID(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            orig(self, ID);

            Options.UpdateAvailableScenes(self.rainWorld);

            foreach (var menu in Options.availableScenes)
            {
                RandomTitleScreen.Logger.LogWarning(menu);
            }

            if (Options.randomizeTitle.Value)
            {
                self.rainWorld.options.titleBackground = Options.availableScenes[Random.Range(0, Options.availableScenes.Count - 1)];
            }

            if (Options.randomizeOptions.Value)
            {
                self.rainWorld.options.subBackground = Options.availableScenes[Random.Range(0, Options.availableScenes.Count - 1)];
            }
        }

        // Unlock all backgrounds
        private static bool BackgroundOptionsMenu_IndexUnlocked(On.MoreSlugcats.BackgroundOptionsMenu.orig_IndexUnlocked orig, MoreSlugcats.BackgroundOptionsMenu self, int ind, List<string> regions)
        {
            if (Options.unlockAllBackgrounds.Value) return true;

            return orig(self, ind, regions);
        }

        private static bool isInit = false;
    }
}
