using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TootTallyLeaderboard
{
    public static class GlobalLeaderboardManager
    {
        private static bool _hasLeaderboardFinishedLoading;

        private static GlobalLeaderboard globalLeaderboard;
        #region HarmonyPatches

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.Start))]
        [HarmonyPostfix]
        public static void OnLevelSelectControllerStartPostfix(List<SingleTrackData> ___alltrackslist, LevelSelectController __instance)
        {
            _hasLeaderboardFinishedLoading = false;
            if (!Plugin.Instance.option.ShowLeaderboard.Value)
            {
                BetterScrollSpeedSliderPatcher.PatchScrollSpeedSlider();
                return;
            }

            globalLeaderboard = new GlobalLeaderboard();
            globalLeaderboard.Initialize(__instance);

            globalLeaderboard.UpdateLeaderboard(__instance, ___alltrackslist, OnUpdateLeaderboardCallback);
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.populateScores))]
        [HarmonyPrefix]
        static bool DontPopulateBaseGameLeaderboard() => !Plugin.Instance.option.ShowLeaderboard.Value;

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.Update))]
        [HarmonyPostfix]
        static void UpdateLoadingSwirlyAnimationOnLevelSelectControllerUpdatePostfix()
        {
            if (globalLeaderboard == null) return;

            if (_hasLeaderboardFinishedLoading)
                globalLeaderboard.UpdateStarRatingAnimation();
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.sortTracks))]
        [HarmonyPostfix]
        static void OnTrackSortReloadLeaderboard(List<SingleTrackData> ___alltrackslist, LevelSelectController __instance)
        {
            if (globalLeaderboard != null && globalLeaderboard.HasLeaderboard)
                UpdateLeaderboardOnAdvanceSongsPostfix(___alltrackslist, __instance);
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.clickPlay))]
        [HarmonyPostfix]
        static void OnLevelSelectControllerClickPlayDeleteLeaderboard(LevelSelectController __instance)
        {
            if (globalLeaderboard == null) return;
            globalLeaderboard.CancelAndClearAllCoroutineInList();
            globalLeaderboard = null;
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.clickBack))]
        [HarmonyPostfix]
        static void OnLevelSelectControllerClickBackDeleteLeaderboard(LevelSelectController __instance)
        {
            if (globalLeaderboard == null) return;
            globalLeaderboard.CancelAndClearAllCoroutineInList();
            globalLeaderboard = null;
        }


        [HarmonyPatch(typeof(LeaderboardManager), nameof(LeaderboardManager.clickTab))]
        [HarmonyPrefix]
        static bool OverwriteClickLeaderboardTabs(int tabindex)
        {
            if (globalLeaderboard != null && globalLeaderboard.HasLeaderboard)
            {
                if (tabindex == 0)
                    if (TootTallyAccounts.TootTallyUser.userInfo.id != 0)
                        globalLeaderboard.OpenUserProfile();
                    else
                        globalLeaderboard.OpenLoginPage();
                else if (tabindex == 1)
                    globalLeaderboard.OpenSongLeaderboard();
                else if (tabindex == 2)
                    globalLeaderboard.ScrollToLocalScore();
                else
                    return true;
            }
            return false;
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.clickNext))]
        [HarmonyPrefix]
        private static bool OnClickNextSkipIfScrollWheelUsed() => ShouldScrollSongs(); //NO SCROLLING WOO
        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.clickPrev))]
        [HarmonyPrefix]
        private static bool OnClickBackSkipIfScrollWheelUsed() => ShouldScrollSongs(); //NO SCROLLING WOO
        private static bool ShouldScrollSongs() => globalLeaderboard == null || !globalLeaderboard.IsMouseOver || Input.mouseScrollDelta.y == 0f; //scroll songs if mouse isn't over the leaderboard and you aren't using mousewheel

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.showButtonsAfterRandomizing))]
        [HarmonyPostfix]

        private static void HideTurboButtonAfterRandomizing(LevelSelectController __instance)
        {
            if (globalLeaderboard == null) return;
            __instance.btnturbo.SetActive(false);
            __instance.btnpractice.SetActive(false);
            __instance.btn_steamleaderboard.SetActive(false);

        }
        #endregion

        #region update

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.doneRandomizing))]
        [HarmonyPostfix]
        private static void OnDoneRandomizingUpdateLeaderboard(List<SingleTrackData> ___alltrackslist, LevelSelectController __instance)
        {
            //randomizing is gonna be true if coming from instant random mod, which doesn't require a leaderboard update
            if (!__instance.randomizing) return;

            if (_hasLeaderboardFinishedLoading)
            {
                _hasLeaderboardFinishedLoading = false;
                globalLeaderboard?.ShowLoadingSwirly();
                globalLeaderboard?.HideSlider();
                globalLeaderboard?.HideErrorText();
                globalLeaderboard?.ClearLeaderboard();
            }

            globalLeaderboard?.UpdateLeaderboard(__instance, ___alltrackslist, OnUpdateLeaderboardCallback);
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.advanceSongs))]
        [HarmonyPostfix]
        static void UpdateLeaderboardOnAdvanceSongsPostfix(List<SingleTrackData> ___alltrackslist, LevelSelectController __instance)
        {
            if (_hasLeaderboardFinishedLoading)
            {
                _hasLeaderboardFinishedLoading = false;
                globalLeaderboard?.ShowLoadingSwirly();
                globalLeaderboard?.HideSlider();
                globalLeaderboard?.HideErrorText();
                globalLeaderboard?.ClearLeaderboard();
            }

            if (__instance.randomizing) return; //Do nothing if randomizing

            globalLeaderboard?.UpdateLeaderboard(__instance, ___alltrackslist, OnUpdateLeaderboardCallback);
        }

        private static void OnUpdateLeaderboardCallback(GlobalLeaderboard.LeaderboardState state)
        {
            switch (state)
            {
                case GlobalLeaderboard.LeaderboardState.None:
                    break;

                case GlobalLeaderboard.LeaderboardState.ErrorNoSongHashFound:
                case GlobalLeaderboard.LeaderboardState.ErrorNoLeaderboardFound:
                    globalLeaderboard.ShowErrorText();
                    break;

                case GlobalLeaderboard.LeaderboardState.ReadyToRefresh:
                    globalLeaderboard.RefreshLeaderboard();
                    break;

                case GlobalLeaderboard.LeaderboardState.ErrorUnexpected:
                    Plugin.LogError("Unexpected Error during leaderboard Update request");
                    break;
            }

            if (state != GlobalLeaderboard.LeaderboardState.SongDataLoaded || state != GlobalLeaderboard.LeaderboardState.SongDataMissing)
            {
                _hasLeaderboardFinishedLoading = true;
                globalLeaderboard.HideLoadingSwirly();
            }

        }
        #endregion
    }
}
