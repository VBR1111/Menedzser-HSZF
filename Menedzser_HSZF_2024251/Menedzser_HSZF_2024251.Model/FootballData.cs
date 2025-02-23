using Newtonsoft.Json;


namespace Menedzser_HSZF_2024251.Model
{
    public class FootballData
    {
        [JsonProperty("teams")]
        public List<TeamJson> Teams { get; set; }

        [JsonProperty("players")]
        public List<PlayerJson> Players { get; set; }

        [JsonProperty("tasks")]
        public List<TaskJson> Tasks { get; set; }
    }

    public class TeamJson
    {
        [JsonProperty("team_id")]
        public int TeamId { get; set; }

        [JsonProperty("team_name")]
        public string TeamName { get; set; }

        [JsonProperty("budget")]
        public decimal Budget { get; set; }

        [JsonProperty("staff_count")]
        public int StaffCount { get; set; }

        [JsonProperty("players")]
        public List<int> Players { get; set; }
    }

    public class PlayerJson
    {
        [JsonProperty("player_id")]
        public int PlayerId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("performance")]
        public string Performance { get; set; }

        [JsonProperty("physical_condition")]
        public string PhysicalCondition { get; set; }

        [JsonProperty("skills")]
        public Dictionary<string, int> Skills { get; set; }
    }

    public class TaskJson
    {
        [JsonProperty("task_id")]
        public int TaskId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("successchance")]
        public string SuccessChance { get; set; }

        [JsonProperty("impact")]
        public Dictionary<string, string> Impact { get; set; }

        [JsonProperty("requirements")]
        public Dictionary<string, int> Requirements { get; set; }
    }
}
