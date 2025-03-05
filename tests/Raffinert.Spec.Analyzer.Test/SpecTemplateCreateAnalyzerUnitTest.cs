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
        public async Task WrongAnonymousSpecTemplateConfiguration()
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
                .WithMessage("The first argument of SpecTemplate.Create must be either an anonymous type projection or class with matching properties (e.g., 'p => new { p.Name }').");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task WrongSpecTemplatePropertyName()
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
                                   p => new Template { Name1 = p.Name },
                                   arg => bananaStringSpec.IsSatisfiedBy(arg.Name1));
                       
                               var categorySpec = specTemplate.Adapt<Category>("cat");
                               var productSpec = specTemplate.Adapt<Product>("prod");
                       
                               Console.WriteLine("Specifications created successfully.");
                           }
                       }

                       class Template
                       {
                           public string Name1 { get; set; }
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
                .WithSpan(12, 33, 12, 38)
                .WithArguments("Property 'Name1' does not exist in sample type 'Product'.");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
