using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = Raffinert.Spec.Analyzer.Test.Verifiers.CSharpAnalyzerVerifier<Raffinert.Spec.Analyzer.SpecTemplateAdaptAnalyzer>;

namespace Raffinert.Spec.Analyzer.Test
{
    [TestClass]
    public class SpecTemplateAdaptAnalyzerUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task NoDiagnostics()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task CategoryHasMissingMembers()
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
                                   p => new { p.Name, p.Id },
                                   arg => bananaStringSpec.IsSatisfiedBy(arg.Name) && arg.Id > 0);
                       
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

            var expected = VerifyCS.Diagnostic("SPEC001").WithSpan(15, 28, 15, 63).WithArguments("Category", "Id");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
