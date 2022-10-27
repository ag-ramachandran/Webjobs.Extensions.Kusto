// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Host.UnitTests
{
    public class PublicSurfaceTests
    {
        [Xunit.Fact]
        public void WebJobsExtensionsKustoVerifyPublicSurfaceArea()
        {
            System.Reflection.Assembly assembly = typeof(Kusto.KustoAttribute).Assembly;

            string[] expected = new[]
            {
                "KustoAttribute",
                "KustoTriggerAttribute",
                "KustoOptions",
                "InitialOffsetOptions",
                "KustoWebJobsBuilderExtensions",
                "KustoWebJobsStartup"
            };

            TestCommon.TestHelpers.AssertPublicTypes(expected, assembly);
        }
    }
}
