namespace Xamasoft.JsonClassGenerator
{
    public interface IJsonClassGeneratorConfig
    {
        string Namespace { get; }
        string SecondaryNamespace { get; }
        bool UseProperties { get; }
        bool InternalVisibility { get; }
        string MainClass { get; }
        bool UsePascalCase { get; }
        bool UseNestedClasses { get; }
        bool ApplyObfuscationAttributes { get; }
        ICodeWriter CodeWriter { get; }
        bool HasSecondaryClasses { get; }
        bool AlwaysUseNullableValues { get; }
        bool UseNamespaces { get; }
        bool ExamplesInDocumentation { get; }
        string PropertyAttribute { get; }
    }
}
