using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = Raffinert.Spec.Analyzer.Test.Verifiers.CSharpAnalyzerVerifier<Raffinert.Spec.Analyzer.SpecTemplateCreateAnalyzer>;

namespace Raffinert.Spec.Analyzer.Test
{
    [TestClass]
    public class SpecTemplateCreateAnalyzerUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task NoDiagnostics()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task WrongSpecTemplateConfiguration()
        {
            var test = """
                       using System;
                       using System.Collections.Generic;
                       using Raffinert.Spec;

                       class Program
                       {
                           static void Main()
                           {
                               var bananaStringSpec = Spec<string>.Create(n => n == "Banana");
                       
                               var specTemplate = SpecTemplate<Product>.Create(
                                   p => p.Name,
                                   arg => bananaStringSpec.IsSatisfiedBy(arg));
                       
                               var categorySpec = specTemplate.Adapt<Category>("cat");
                               var productSpec = specTemplate.Adapt<Product>("prod");
                       
                               Console.WriteLine("Specifications created successfully.");
                           }
                       }

                       public class Product
                       {
                           public int Id { get; set; }
                           public string Name { get; set; }
                           public decimal Price { get; set; }
                           public int CategoryId { get; set; }
                           public Category Category { get; set; }
                       }

                       public class Category
                       {
                           public string Name { get; set; }
                           public ICollection<Product> Products { get; set; } = new List<Product>();
                       }
                       """;

            var expected = VerifyCS.Diagnostic("SPEC002")
                .WithSpan(12, 13, 12, 24)
                .WithMessage("The first argument of SpecTemplate.Create must be an anonymous type projection (e.g., 'p => new { p.Name }').");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
