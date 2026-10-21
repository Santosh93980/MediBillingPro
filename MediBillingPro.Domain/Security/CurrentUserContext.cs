namespace MediBillingPro.Domain.Security
{
    public static class CurrentUserContext
    {
        public static int? UserId { get; private set; }
        public static string UserName { get; private set; } = "SYSTEM";
        public static void Set(int? userId, string userName)
        {
            UserId = userId;
            UserName = string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName;
        }
    }
}
