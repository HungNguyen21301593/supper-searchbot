using Newtonsoft.Json;

namespace supper_searchbot.Entity
{
    public class ExecutorSetting
    {
        public int ID { get; set; }
        public User User { get; set; }
        public Status Status { get; set; }
        public string Type { get; set; }
        public string SettingJson { get; set; }
        public List<History> Histories { get; set; }
        public DateTime LastExcuted { get; set; }
    }

    public class Setting
    {
        public string Type { get; set; }
        public int StartPage { get; set; }
        public int EndPage { get; set; }
        public int MinAdsPositionOnEachPage { get; set; }
        public int MaximumAdsOnEachPage { get; set; }
        public List<string> MustHaveKeywords { get; set; }
        public List<string> Keywords { get; set; }
        public List<string> ExcludeKeywords { get; set; }
        public List<string> TelegramIds { get; set; }
        public List<string> DumbTelegramIds { get; set; }
        public BaseUrlSetting BaseUrlSetting { get; set; }

        public static Setting FromString(string SettingJson)
        {
            return JsonConvert.DeserializeObject<Setting>(SettingJson);
        }

        public static string ToString(Setting Setting)
        {
            return JsonConvert.SerializeObject(Setting);
        }
    }

    public enum Status
    {
        Ready,
        Started
    }

    public class BaseUrlSetting
    {
        public List<string> CriteriaUrls { get; set; }
        public DynamicParam DynamicParams { get; set; }
        public Dictionary<string, string> StaticParams { get; set; }
    }

    public class DynamicParam
    {
        public string Page { get; set; }
    }

    public class DynamicParamValue : DynamicParam
    {
    }

    public static class ParamExtension
    {
        public static string Apply(this Dictionary<string, string> staticParams, string criteriaUrl)
        {
            var result = criteriaUrl;
            foreach (var staticParam in staticParams)
            {
                result = result.Replace(staticParam.Key, staticParam.Value);
            }
            return result;
        }
    }

    public static class ExecutorType
    {
        public const string ExecutorTypeKijiji = "Kijiji";
    }
}
