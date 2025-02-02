namespace Start_TooltipFix;

[AttributeUsage(AttributeTargets.Class)]
public class RequireAdminAttribute : Attribute
{
    private RequireAdminType _requireAdmin;

    public RequireAdminAttribute(RequireAdminType requireAdmin)
    {
        _requireAdmin = requireAdmin;
    }
}
public enum RequireAdminType
{
    None,
    Admin,
    External
}