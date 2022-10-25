// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.TestCommon;
using Microsoft.Azure.WebJobs.Kusto;
using Xunit;

namespace Microsoft.Azure.WebJobs.Host.UnitTests
{
    public class PublicSurfaceTests
    {
        [Fact]
        public void WebJobs_Extensions_Kusto_VerifyPublicSurfaceArea()
        {
            var assembly = typeof(KustoAttribute).Assembly;

            var expected = new[]
            {
                "KustoAttribute",
                "KustoTriggerAttribute",
                "KustoOptions",
                "InitialOffsetOptions",
                "KustoWebJobsBuilderExtensions",
                "KustoWebJobsStartup"
            };

            TestHelpers.AssertPublicTypes(expected, assembly);
        }
    }
}
