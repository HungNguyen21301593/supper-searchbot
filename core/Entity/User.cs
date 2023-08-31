using Telegram.Bot;

namespace supper_searchbot.Entity
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public TelegramBot TelegramBot { get; set; }
        public List<ExecutorSetting> ExecutorSettings { get; set; }
    }
}