using SnowyRiver.VisionMaster.Services.Interfaces;

namespace SnowyRiver.VisionMaster.Services
{
    public class MessageService : IMessageService
    {
        public string GetMessage()
        {
            return "Hello from the Message Service";
        }
    }
}
