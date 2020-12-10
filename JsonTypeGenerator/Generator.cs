using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JsonTypeGenerator.JsonCSharpClassGeneratorLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xamasoft.JsonClassGenerator;


namespace JsonTypeGenerator
{
    [Generator]
    public class Generator : ISourceGenerator
    {
        private const string attributeText = @"using System;
namespace JsonTypeGenerator
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class JsonTypeAttribute : Attribute
    {
        public JsonTypeAttribute()
        {
        }
        public string ClassName { get; set; }
    }
}";
        
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("Extension", SourceText.From(attributeText, Encoding.UTF8));
            
            // retrieve the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            // we're going to create a new compilation that contains the attribute.
            // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            var options = (context.Compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(attributeText, Encoding.UTF8), options));

            var attributeSymbol = compilation.GetTypeByMetadataName("JsonTypeGenerator.JsonTypeAttribute");
            
            // loop over the candidate fields, and keep the ones that are actually annotated
            var fieldSymbols = new List<IFieldSymbol>();
            foreach (var field in receiver.CandidateFields)
            {
                var model = compilation.GetSemanticModel(field.SyntaxTree);
                foreach (var variable in field.Declaration.Variables)
                {
                    // Get the symbol being decleared by the field, and keep it if its annotated
                    var fieldSymbol = ModelExtensions.GetDeclaredSymbol(model, variable) as IFieldSymbol;
                    if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                    {
                        fieldSymbols.Add(fieldSymbol);
                    }
                }
            }

            foreach (var fieldSymbol in fieldSymbols)
            {
                var attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
                var className = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "ClassName").Value.Value?.ToString();
                var fieldText = fieldSymbol.ConstantValue.ToString();
                
                using var sw = new StringWriter();
                var json = fieldText;
                var gen = new JsonClassGenerator
                {
                    Example = json,
                    OutputStream = sw,
                    UsePascalCase = true,
                    MainClass = className ?? "Root",
                    Namespace = "JsonTypeGenerator.Json"
                };
                gen.GenerateClasses();
            
                context.AddSource("GeneratedTypes", sw.ToString());
            }
        }
        
        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<FieldDeclarationSyntax> CandidateFields { get; } = new();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is FieldDeclarationSyntax fieldDeclarationSyntax
                    && fieldDeclarationSyntax.AttributeLists.Count > 0)
                {
                    CandidateFields.Add(fieldDeclarationSyntax);
                }
            }
        }
    }
}