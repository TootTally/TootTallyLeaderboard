using BepInEx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TootTallyCore.Utils.Helpers;
using UnityEngine;

namespace TootTallyLeaderboard.Replays
{
    public static class CachedReplays
    {
        private static readonly string FILE_PATH = Path.Combine(FileHelper.FILE_PATH_TOOTTALLY_APPDATA, "TootTallyCachedReplays.json");
        private static readonly string REPLAY_PATH = Path.Combine(Paths.BepInExRootPath, "Replays");

        public static List<CachedReplayData> songhashToCachedReplayData;

        public static List<CachedReplayData> GetReplayPathsFromSongHash(string songHash) => songhashToCachedReplayData.Where(x => x.songhash == songHash).ToList();

        public static void LoadCachedReplays(bool processNewReplays = true)
        {
            songhashToCachedReplayData = FileHelper.LoadFromTootTallyAppData<List<CachedReplayData>>(FILE_PATH);
            if (songhashToCachedReplayData == default)
            {
                Plugin.LogInfo($"Couldn't find replay cache file, creating new one!");
                songhashToCachedReplayData = new List<CachedReplayData>();
                FileHelper.SaveToTootTallyAppData(FILE_PATH, songhashToCachedReplayData, true);
            }

            if (processNewReplays)
            {
                var sw = Stopwatch.StartNew();
                var newReplays = Directory.GetFiles(REPLAY_PATH)
                    .Where(filePath => filePath.Contains(".ttr") &&
                    !songhashToCachedReplayData.Any(replayData => replayData.filePath == filePath)).ToList();

                if (newReplays.Count == 0)
                {
                    Plugin.LogInfo($"No new replays found. Skipping cache update!");
                    return;
                }
                for (int i = 0; i < newReplays.Count; i++)
                {
                    try
                    {
                        var parsedReplayData = JsonConvert.DeserializeObject<CachedReplayData>(FileHelper.ReadJsonFromFile("", newReplays[i]));
                        parsedReplayData.filePath = newReplays[i];
                        songhashToCachedReplayData.Add(parsedReplayData);
                    }
                    catch (Exception ex)
                    {
                        Plugin.LogError($"Couldn't parse replay data for {newReplays[i]}: {ex.Message} - {ex.StackTrace}");
                        continue;
                    }
                }
                Plugin.LogInfo($"Processed {newReplays.Count} new replays in {sw.Elapsed.TotalMilliseconds}ms.");
                SaveCachedReplays();
            }

        }

        public static void SaveCachedReplays()
        {
            FileHelper.SaveToTootTallyAppData(FILE_PATH, songhashToCachedReplayData, false);
        }

        [Serializable]
        public class CachedReplayData
        {
            public string filePath { get; set; }
            public string songhash { get; set; }

        }
    }
}
