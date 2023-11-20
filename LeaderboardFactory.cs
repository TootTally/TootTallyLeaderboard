using TMPro;
using UnityEngine.UI;
using UnityEngine;
using TootTallyCore.Graphics;
using TootTallyCore.APIServices;
using TootTallyLeaderboard.Replays;
using HarmonyLib;

namespace TootTallyLeaderboard
{
    public static class LeaderboardFactory
    {
        private static TMP_Text _leaderboardHeaderPrefab, _leaderboardTextPrefab;
        private static GameObject _steamLeaderboardPrefab, _singleScorePrefab, _panelBodyPrefab;
        private static LeaderboardRowEntry _singleRowPrefab;

        [HarmonyPatch(typeof(GameObjectFactory), nameof(GameObjectFactory.OnLevelSelectControllerInitialize))]
        [HarmonyPostfix]
        public static void OnLevelSelectStart(LevelSelectController levelSelectController)
        {
            SetSteamLeaderboardPrefab();
            SetSingleScorePrefab();
            SetLeaderboardHeaderPrefab();
            SetLeaderboardTextPrefab();
            SetSingleRowPrefab();
        }

        private static void SetSteamLeaderboardPrefab()
        {
            GameObject camerapopups = GameObject.Find("Camera-Popups").gameObject;
            GameObject steamLeaderboardCanvas = camerapopups.transform.Find("LeaderboardCanvas").gameObject;

            _steamLeaderboardPrefab = GameObject.Instantiate(steamLeaderboardCanvas);
            _steamLeaderboardPrefab.name = "CustomLeaderboardCanvas";
            _steamLeaderboardPrefab.SetActive(true); //Has to be set to true else it crashes when yoinking other objects?? #UnityStuff

            //Don't think we need these...
            GameObjectFactory.DestroyFromParent(_steamLeaderboardPrefab, "BG");
            GameObject.DestroyImmediate(_steamLeaderboardPrefab.GetComponent<CanvasScaler>());

            RectTransform lbCanvasRect = _steamLeaderboardPrefab.GetComponent<RectTransform>();
            lbCanvasRect.anchoredPosition = new Vector2(237, -311);
            lbCanvasRect.localScale = Vector2.one * 0.5f;

            SetPanelBodyInSteamLeaderboard();

            GameObject.DontDestroyOnLoad(_steamLeaderboardPrefab);
        }

        private static void SetPanelBodyInSteamLeaderboard()
        {
            _panelBodyPrefab = _steamLeaderboardPrefab.transform.Find("PanelBody").gameObject;
            _panelBodyPrefab.SetActive(true);

            RectTransform panelRectTransform = _panelBodyPrefab.GetComponent<RectTransform>();
            panelRectTransform.anchoredPosition = Vector2.zero;
            panelRectTransform.sizeDelta = new Vector2(750, 300);

            //We dont need these right?
            GameObjectFactory.DestroyFromParent(_panelBodyPrefab, "CloseButton");
            GameObjectFactory.DestroyFromParent(_panelBodyPrefab, "txt_legal");
            GameObjectFactory.DestroyFromParent(_panelBodyPrefab, "txt_leaderboards");
            GameObjectFactory.DestroyFromParent(_panelBodyPrefab, "txt_songname");
            GameObjectFactory.DestroyFromParent(_panelBodyPrefab, "rule");
            GameObjectFactory.DestroyFromParent(_panelBodyPrefab, "HelpBtn");
            GameObjectFactory.DestroyFromParent(_panelBodyPrefab, "loadingspinner_parent");

            SetTabsInPanelBody();
            SetErrorsInPanelBody();
            SetScoreboardInPanelBody();
            AddSliderInPanelBody();
        }

        private static void SetTabsInPanelBody()
        {
            GameObject tabs = _panelBodyPrefab.transform.Find("tabs").gameObject;
            tabs.SetActive(false); //Hide until icons are loaded
            GameObject.DestroyImmediate(tabs.GetComponent<HorizontalLayoutGroup>());
            for (int i = 0; i < 3; i++)
            {
                GameObject currentTab = _steamLeaderboardPrefab.GetComponent<LeaderboardManager>().tabs[i];
                GameObjectFactory.DestroyFromParent(currentTab, "label");
                GameObjectFactory.DestroyFromParent(currentTab, "rule");

                RectTransform tabRect = currentTab.GetComponent<RectTransform>();
                tabRect.anchoredPosition = new Vector2(15, -40);
                tabRect.sizeDelta = new Vector2(40, 40);
            }
            VerticalLayoutGroup verticalLayout = tabs.AddComponent<VerticalLayoutGroup>();
            verticalLayout.childForceExpandWidth = false;
            verticalLayout.childScaleWidth = verticalLayout.childScaleHeight = false;
            verticalLayout.childControlWidth = verticalLayout.childControlHeight = false;
            verticalLayout.padding.left = 20;
            verticalLayout.padding.top = 36;

            RectTransform tabsRectTransform = tabs.GetComponent<RectTransform>();
            tabsRectTransform.anchoredPosition = new Vector2(328, -10);
            tabsRectTransform.sizeDelta = new Vector2(-676, 280);
        }

        private static void SetErrorsInPanelBody()
        {
            GameObject errorsHolder = _panelBodyPrefab.transform.Find("errors").gameObject;

            RectTransform errorsTransform = errorsHolder.GetComponent<RectTransform>();
            errorsTransform.anchoredPosition = new Vector2(-30, 15);
            errorsTransform.sizeDelta = new Vector2(-200, 0);

            errorsHolder.SetActive(false);

            errorsHolder.transform.Find("error_noleaderboard").gameObject.SetActive(true);
        }

        private static void SetScoreboardInPanelBody()
        {
            GameObject scoresbody = _panelBodyPrefab.transform.Find("scoresbody").gameObject;

            RectTransform scoresbodyRectTransform = scoresbody.GetComponent<RectTransform>();
            scoresbodyRectTransform.anchoredPosition = new Vector2(0, -10);
            scoresbodyRectTransform.sizeDelta = Vector2.one * -20;

            GameObject scoreboard = _panelBodyPrefab.transform.Find("scoreboard").gameObject; //Single scores goes in there

            scoreboard.AddComponent<RectMask2D>();
            RectTransform scoreboardRectTransform = scoreboard.GetComponent<RectTransform>();
            scoreboardRectTransform.anchoredPosition = new Vector2(-30, -10);
            scoreboardRectTransform.sizeDelta = new Vector2(-80, -20);
        }

        private static void AddSliderInPanelBody()
        {
            GameObjectFactory.CreateVerticalSliderFromPrefab(_panelBodyPrefab.transform, "LeaderboardVerticalSlider");
        }

        private static void SetSingleScorePrefab()
        {
            GameObject singleScore = _panelBodyPrefab.transform.Find("scoreboard/SingleScore").gameObject;
            _singleScorePrefab = GameObject.Instantiate(singleScore);
            _singleScorePrefab.name = "singleScorePrefab";
            _singleScorePrefab.GetComponent<RectTransform>().sizeDelta = new Vector2(_singleScorePrefab.GetComponent<RectTransform>().sizeDelta.x, 35);

            //find image. set the size and position and always enable the image
            GameObject imageGameObject = _singleScorePrefab.transform.Find("Image").gameObject;
            LayoutElement layoutElement = imageGameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            RectTransform imageRectTransform = imageGameObject.GetComponent<RectTransform>();
            imageRectTransform.sizeDelta = new Vector2(-5, 0);
            imageRectTransform.anchoredPosition = new Vector2(-10, 0);

            Image image = imageGameObject.GetComponent<Image>();
            image.enabled = true;
            image.maskable = true;

            _singleScorePrefab.gameObject.SetActive(false);

            GameObject.DontDestroyOnLoad(_singleScorePrefab.gameObject);
        }

        private static void SetLeaderboardHeaderPrefab()
        {
            Text tempHeaderTxt = GameObject.Instantiate(_singleScorePrefab.transform.Find("Num").GetComponent<Text>());
            _leaderboardHeaderPrefab = GameObject.Instantiate(GameObjectFactory.GetComfortaaFont);
            _leaderboardHeaderPrefab.name = "LeaderboardHeaderPrefab";
            _leaderboardHeaderPrefab.alignment = TextAlignmentOptions.Center;
            _leaderboardHeaderPrefab.maskable = true;
            _leaderboardHeaderPrefab.enableWordWrapping = false;
            _leaderboardHeaderPrefab.gameObject.SetActive(true);
            _leaderboardHeaderPrefab.enableAutoSizing = true;
            _leaderboardHeaderPrefab.fontSizeMax = _leaderboardHeaderPrefab.fontSize;

            GameObject.DestroyImmediate(tempHeaderTxt.gameObject);
            GameObject.DontDestroyOnLoad(_leaderboardHeaderPrefab.gameObject);
        }

        private static void SetLeaderboardTextPrefab()
        {
            Text tempTxt = GameObject.Instantiate(_singleScorePrefab.transform.Find("Name").GetComponent<Text>());
            _leaderboardTextPrefab = GameObject.Instantiate(GameObjectFactory.GetComfortaaFont);
            _leaderboardTextPrefab.name = "LeaderboardTextPrefab";
            _leaderboardTextPrefab.alignment = TextAlignmentOptions.Center;
            _leaderboardTextPrefab.maskable = true;
            _leaderboardTextPrefab.enableWordWrapping = false;
            _leaderboardTextPrefab.gameObject.SetActive(true);
            _leaderboardTextPrefab.color = Color.white;
            _leaderboardTextPrefab.enableAutoSizing = true;
            _leaderboardTextPrefab.fontSizeMax = _leaderboardTextPrefab.fontSize;


            DestroyNumNameScoreFromSingleScorePrefab();

            GameObject.DestroyImmediate(tempTxt.gameObject);
            GameObject.DontDestroyOnLoad(_leaderboardTextPrefab.gameObject);
        }

        private static void DestroyNumNameScoreFromSingleScorePrefab()
        {
            GameObjectFactory.DestroyFromParent(_singleScorePrefab, "Num");
            GameObjectFactory.DestroyFromParent(_singleScorePrefab, "Name");
            GameObjectFactory.DestroyFromParent(_singleScorePrefab, "Score");
        }

        private static void SetSingleRowPrefab()
        {
            _singleRowPrefab = _singleScorePrefab.AddComponent<LeaderboardRowEntry>();
            TMP_Text rank = GameObject.Instantiate(_leaderboardHeaderPrefab, _singleScorePrefab.transform);
            rank.name = "rank";
            TMP_Text username = GameObject.Instantiate(_leaderboardTextPrefab, _singleScorePrefab.transform);
            username.name = "username";
            TMP_Text score = GameObject.Instantiate(_leaderboardTextPrefab, _singleScorePrefab.transform);
            score.name = "score";
            TMP_Text percent = GameObject.Instantiate(_leaderboardTextPrefab, _singleScorePrefab.transform);
            percent.name = "percent";
            TMP_Text grade = GameObject.Instantiate(_leaderboardTextPrefab, _singleScorePrefab.transform);
            grade.name = "grade";
            TMP_Text maxcombo = GameObject.Instantiate(_leaderboardTextPrefab, _singleScorePrefab.transform);
            maxcombo.name = "maxcombo";
            _singleRowPrefab.ConstructLeaderboardEntry(_singleScorePrefab, rank, username, score, percent, grade, maxcombo, false);
            _singleRowPrefab.singleScore.name = "singleRowPrefab";
        }

        public static GameObject CreateSteamLeaderboardFromPrefab(Transform canvasTransform, string name)
        {
            GameObject steamLeaderboard = GameObject.Instantiate(_steamLeaderboardPrefab, canvasTransform);
            steamLeaderboard.name = name;
            return steamLeaderboard;
        }

        public static LeaderboardRowEntry CreateLeaderboardRowEntryFromScore(Transform canvasTransform, string name, SerializableClass.ScoreDataFromDB scoreData, int count, Color gradeColor, LevelSelectController levelSelectControllerInstance)
        {
            LeaderboardRowEntry rowEntry = GameObject.Instantiate(_singleRowPrefab, canvasTransform);
            rowEntry.name = name;
            rowEntry.username.text = scoreData.player;
            rowEntry.score.text = string.Format("{0:n0}", scoreData.score) + $" ({scoreData.replay_speed:0.00}x)";
            rowEntry.score.gameObject.AddComponent<BubblePopupHandler>().Initialize(GameObjectFactory.CreateBubble(new Vector2(175, 200), $"{rowEntry.name}ScoreBubble", GetTallyBubbleText(scoreData.GetTally), 10, false));
            rowEntry.rank.text = "#" + count;
            rowEntry.percent.text = scoreData.percentage.ToString("0.00") + "%";
            rowEntry.grade.text = scoreData.grade;
            if (scoreData.grade == "SS")
            {
                rowEntry.grade.text = "S";
                GameObjectFactory.CreateDoubleText(rowEntry.grade.transform, "DoubleS" + scoreData.player + "Text", "S", Color.yellow);

            }
            else if (scoreData.grade == "SSS")
            {
                rowEntry.grade.text = "S";
                GameObjectFactory.CreateTripleText(rowEntry.grade.transform, "TripleS" + scoreData.player + "Text", "S", Color.yellow);
            }
            else
                rowEntry.grade.color = gradeColor;
            if (scoreData.is_rated)
            {

                rowEntry.maxcombo.text = (int)scoreData.tt + "tt";
                rowEntry.maxcombo.gameObject.AddComponent<BubblePopupHandler>().Initialize(GameObjectFactory.CreateBubble(new Vector2(150, 75), $"{rowEntry.name}ComboBubble", $"{scoreData.max_combo} combo", 10, true));
            }
            else
                rowEntry.maxcombo.text = scoreData.max_combo + "x";
            rowEntry.replayId = scoreData.replay_id;
            rowEntry.rowId = count;
            rowEntry.singleScore.AddComponent<CanvasGroup>();
            HorizontalLayoutGroup layoutGroup = rowEntry.singleScore.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childForceExpandWidth = layoutGroup.childForceExpandHeight = false;
            layoutGroup.childScaleWidth = layoutGroup.childScaleHeight = false;
            layoutGroup.childControlWidth = layoutGroup.childControlHeight = false;
            layoutGroup.spacing = 8;
            layoutGroup.padding.left = 8;
            layoutGroup.padding.top = 2;
            rowEntry.singleScore.SetActive(true);
            rowEntry.singleScore.transform.Find("Image").gameObject.SetActive(count % 2 == 0);

            var replayId = rowEntry.replayId;
            if (replayId != "" && replayId != null) //if there's a uuid, add a replay button
            {
                GameObjectFactory.CreateCustomButton(rowEntry.singleScore.transform, Vector2.zero, new Vector2(26, 26), "►", "ReplayButton",
                delegate
                {
                    Plugin.LogInfo("ID:" + replayId);
                    ReplaySystemManager.ResolveLoadReplay(replayId, levelSelectControllerInstance);
                });
            }
            return rowEntry;
        }

        private static string GetTallyBubbleText(int[] tally) =>
            tally != null ? $"Perfect: {tally[4]}\n" +
                            $"Nice: {tally[3]}\n" +
                            $"Okay: {tally[2]}\n" +
                            $"Meh: {tally[1]}\n" +
                            $"Nasty: {tally[0]}\n" : "No Tally";

        public static GameObject CreateDefaultPanel(Transform canvasTransform, Vector2 anchoredPosition, Vector2 size, string name)
        {
            GameObject panel = GameObject.Instantiate(_panelBodyPrefab, canvasTransform);
            panel.name = name;
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            rectTransform.localScale = Vector2.one * .5f;

            GameObjectFactory.DestroyFromParent(panel, "scoreboard");
            GameObjectFactory.DestroyFromParent(panel, "tabs");
            GameObjectFactory.DestroyFromParent(panel, "errors");
            GameObjectFactory.DestroyFromParent(panel, "LeaderboardVerticalSlider");

            return panel;
        }
    }
}
