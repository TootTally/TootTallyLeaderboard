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
using TootTallyCore.Utils.TootTallyNotifs;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.Util;

namespace TootTallyLeaderboard.Replays
{
    public static class CachedReplays
    {
        private static readonly string FILE_PATH = Path.Combine(FileHelper.FILE_PATH_TOOTTALLY_APPDATA, "TootTallyCachedReplays.json");
        private static readonly string FAILED_FILE_PATH = Path.Combine(FileHelper.FILE_PATH_TOOTTALLY_APPDATA, "TootTallyFailedCachedReplays.json");
        private static readonly string REPLAY_PATH = Path.Combine(Paths.BepInExRootPath, "Replays");

        public static List<CachedReplayData> songhashToCachedReplayData;
        public static List<string> failedCachedReplayPaths;

        public static bool isLoading;

        public static List<CachedReplayData> GetReplayPathsFromSongHash(string songHash) => songhashToCachedReplayData.Where(x => x.songhash == songHash).ToList();
        public static void DeleteReplayFromSongHash(CachedReplayData data)
        {
            songhashToCachedReplayData.Remove(data);
            SaveCachedReplays();
        }

        public static void LoadCachedReplays(bool processNewReplays = true)
        {
            songhashToCachedReplayData = FileHelper.LoadFromTootTallyAppData<List<CachedReplayData>>(FILE_PATH);
            if (songhashToCachedReplayData == default)
            {
                Plugin.LogInfo($"Couldn't find replay cache file, creating new one!");
                songhashToCachedReplayData = new List<CachedReplayData>();
                FileHelper.SaveToTootTallyAppData(FILE_PATH, songhashToCachedReplayData, true);
            }
            failedCachedReplayPaths = FileHelper.LoadFromTootTallyAppData<List<string>>(FAILED_FILE_PATH);
            if (failedCachedReplayPaths == default)
            {
                Plugin.LogInfo($"Couldn't find failed replay cache file, creating new one!");
                failedCachedReplayPaths = new List<string>();
                FileHelper.SaveToTootTallyAppData(FAILED_FILE_PATH, failedCachedReplayPaths, true);
            }

            if (!processNewReplays || isLoading) return;
            isLoading = true;
            Plugin.Instance.StartCoroutine(ProcessReplays());
        }

        public static IEnumerator<UnityWebRequest> ProcessReplays()
        {
            var sw = Stopwatch.StartNew();
            var newReplays = Directory.GetFiles(REPLAY_PATH)
                .Where(filePath => filePath.Contains(".ttr"))
                .Except(songhashToCachedReplayData.Select(data => data.filePath))
                .Except(failedCachedReplayPaths).ToList();
            /*var newReplays = Directory.GetFiles(REPLAY_PATH)
                .Where(filePath => filePath.Contains(".ttr") &&
                !songhashToCachedReplayData.Any(replayData => replayData.filePath == filePath) &&
                !failedCachedReplayPaths.Contains(filePath)).ToList();*/

            if (newReplays.Count == 0)
            {
                Plugin.LogInfo($"No new replays found. Skipping cache update!");
                yield break;
            }
            var notif = TootTallyNotifManager.ManualNotif($"Loading: 0 / {newReplays.Count} replays loaded.", Color.white);
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
                    failedCachedReplayPaths.Add(newReplays[i]);
                    continue;
                }
                notif.SetText($"Loading: {i} / {newReplays.Count} replays loaded.");
                yield return null;
            }
            notif.Dispose();
            TootTallyNotifManager.DisplayNotif($"Processed {newReplays.Count} new replays in {sw.Elapsed.TotalMilliseconds}ms.");
            Plugin.LogInfo($"Processed {newReplays.Count} new replays in {sw.Elapsed.TotalMilliseconds}ms.");
            SaveFailedCachedReplays();
            SaveCachedReplays();
            isLoading = false;
        }

        public static void AddReplayIfNotExist(string path, string songhash)
        {
            if (songhashToCachedReplayData.Any(x => x.filePath == path)) return;
            var data = new CachedReplayData() { filePath = path, songhash = songhash };
            songhashToCachedReplayData.Add(data);
            SaveCachedReplays();
        }

        public static void SaveFailedCachedReplays()
        {
            FileHelper.SaveToTootTallyAppData(FAILED_FILE_PATH, failedCachedReplayPaths, false);
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
