using System.IO;
using BepInEx;
using BepInEx.Configuration;
using TootTallyCore;
using UnityEngine;
using UnityEngine.UI;

namespace TootTallyLeaderboard
{
    public static class BetterScrollSpeedSliderPatcher
    {
        private const string CONFIG_NAME = "BetterScrollSpeed.cfg";
        private const string CONFIG_FIELD = "SliderValues";
        private const uint DEFAULT_MAX = 250;
        private const uint DEFAULT_MIN = 5;
        private const float DEFAULT_VALUE = 100;

        public static Options options;

        public static void PatchScrollSpeedSlider()
        {
            SetSliderOption();

            Text yoinkText = GameObject.Find("MainCanvas/FullScreenPanel/ScrollSpeed-lbl").GetComponent<Text>();

            Slider slider = GameObject.Find("MainCanvas/FullScreenPanel/Slider").GetComponent<Slider>();
            slider.navigation = new Navigation() { mode = Navigation.Mode.None };
            slider.fillRect.gameObject.GetComponent<Image>().color = Theme.colors.scrollSpeedSlider.fill;
            slider.transform.Find("Background").GetComponent<Image>().color = Theme.colors.scrollSpeedSlider.background;
            slider.handleRect.gameObject.GetComponent<Image>().color = Theme.colors.scrollSpeedSlider.handle;
            slider.minValue = options.Min.Value / 100f;
            slider.maxValue = options.Max.Value / 100f;
            slider.value = options.LastValue.Value / 100f;

            Text scrollSpeedSliderText = GameObject.Instantiate(yoinkText, slider.handleRect);

            scrollSpeedSliderText.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            scrollSpeedSliderText.alignment = TextAnchor.MiddleCenter;
            scrollSpeedSliderText.fontSize = 12;
            scrollSpeedSliderText.text = ((int)(GlobalVariables.gamescrollspeed * 100)).ToString();
            scrollSpeedSliderText.color = Theme.colors.scrollSpeedSlider.text;
            scrollSpeedSliderText.text = SliderValueToText(slider.value);
            slider.onValueChanged.AddListener((float _value) => { options.LastValue.Value = _value * 100f; scrollSpeedSliderText.text = SliderValueToText(_value); });

            GameObject.Find("MainCanvas/FullScreenPanel/ScrollSpeed-lbl").gameObject.SetActive(false);

        }

        public static void SetSliderOption()
        {
            if (options != null) return;

            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true);
            options = new Options()
            {
                Max = config.Bind(CONFIG_FIELD, nameof(options.Max), DEFAULT_MAX),
                Min = config.Bind(CONFIG_FIELD, nameof(options.Min), DEFAULT_MIN),
                LastValue = config.Bind(CONFIG_FIELD, nameof(options.LastValue), DEFAULT_VALUE)
            };
            if (options.Min.Value >= options.Max.Value)
                Plugin.LogError("Slider MAX has to be greater than Slider MIN");
            else if (options.Min.Value <= 4)
                Plugin.LogError("Slider MIN has to be greater or equal to 5");
            else if (options.Max.Value >= 1000)
                Plugin.LogError("Buddy. What are you trying to do?? You're never gonna play with 1k+ scrollspeed...");

            if (options.Max.Value >= options.Min.Value || options.Min.Value <= 4 || options.Max.Value >= 1000)
            {
                options.Min.Value = DEFAULT_MIN;
                options.Max.Value = DEFAULT_MAX;
            }

            if (options.LastValue.Value < options.Min.Value || options.LastValue.Value > options.Max.Value) //Don't even try...
                options.LastValue.Value = DEFAULT_VALUE;
        }

        public static string SliderValueToText(float value)
        {
            if (value >= 1)
                return Mathf.FloorToInt(value * 100).ToString();
            else
                return Mathf.CeilToInt(value * 100).ToString();
        }
    }
    public class Options
    {
        public ConfigEntry<uint> Max { get; set; }
        public ConfigEntry<uint> Min { get; set; }
        public ConfigEntry<float> LastValue { get; set; }

    }
}
