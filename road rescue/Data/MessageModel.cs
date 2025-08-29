using System.Windows.Input;

namespace road_rescue.Data
{
    public class MessageModel
    {
        public string ProfileImage { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string MessagePreview { get; set; } = string.Empty;
        public string TimeSent { get; set; } = string.Empty;
        public ICommand TapCommand { get; set; } = null!;
    }
}
