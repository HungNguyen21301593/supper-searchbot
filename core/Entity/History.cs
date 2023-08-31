namespace supper_searchbot.Entity
{
    public class History
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime Created { get; set; }

        public int ExecutorSettingId { get; set; }
        public ExecutorSetting ExecutorSetting { get; set; }
    }
}