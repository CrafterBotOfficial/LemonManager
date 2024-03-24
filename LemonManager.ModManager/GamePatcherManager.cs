using AssetsTools.NET.Extra;
using LemonManager.ModManager.Models;
using MelonLoaderInstaller.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonManager.ModManager
{
    /// <summary> Manages the LemonLoader.MelonInstallerCore project</summary>
    public class GamePatcherManager
    {
        private Patcher patcher;
        private GamePatcherManager(PatchArguments patchArguments)
        {

        }

        public static async void PatchApp(UnityApplicationInfoModel info)
        {
            var assetManager = new AssetsManager();
            

            new GamePatcherManager();
        }
    }
}
