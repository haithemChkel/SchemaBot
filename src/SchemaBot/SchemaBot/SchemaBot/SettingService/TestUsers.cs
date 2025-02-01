// Program.cs
public static class TestUsers
{
    public static List<TestUser> Users = new() {
        new TestUser("admin", "admin123", "Admin"),
        new TestUser("editor", "editor123", "Editor"),
        new TestUser("viewer", "viewer123", "Viewer")
    };
}
