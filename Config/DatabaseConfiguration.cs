namespace SquidORM.Config;

public record DatabaseConfiguration(string Host, string User, string Password)
{
    private const int DefaultMysqlPort = 3306;

    public string? DatabaseName { get; init; } = null;

    public int Port { get; init; } = DefaultMysqlPort;

    public bool RecordCaching { get; init; } = false;

    public bool UseDirtySystem { get; init; } = false;

    public TableCreationMod? TableCreationMod { get; init; } = null;

    public string ConnectionString =>
            $"server={Host};port={Port};user={User};password={Password};database={DatabaseName};";
}
