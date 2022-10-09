namespace MyTgBot.Model {
    /// <summary>
    /// Exists as a separator information between users.
    /// Extremely dubious solution.
    /// </summary>
    class User {
        public int OperationLevel { get; set; } = 0;

        // stub, in a real version there will be a call to the DB
        public List<NotifierData> userNotifies { get; private set; } = new();
        public int NotifyCount => userNotifies.Count;

    }
}
