namespace supper_searchbot.Entity
{
    public class TelegramBot
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public string Token { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
    }
}