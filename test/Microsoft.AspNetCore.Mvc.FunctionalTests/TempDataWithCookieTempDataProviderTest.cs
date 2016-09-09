// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class TempDataWithCookieTempDataProviderTest : IClassFixture<MvcTestFixture<BasicWebSite.StartupWithCookieTempDataProvider>>
    {
        private readonly TempDataCommon _tempDataCommonTest;

        public TempDataWithCookieTempDataProviderTest(MvcTestFixture<BasicWebSite.StartupWithCookieTempDataProvider> fixture)
        {
            _tempDataCommonTest = new TempDataCommon(fixture.Client);
        }

        [Fact]
        public Task PersistsJustForNextRequest()
        {
            return _tempDataCommonTest.PersistsJustForNextRequest();
        }

        [Fact]
        public Task ViewRendersTempData()
        {
            return _tempDataCommonTest.ViewRendersTempData();
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/21
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public Task Redirect_RetainsTempData_EvenIfAccessed()
        {
            return _tempDataCommonTest.Redirect_RetainsTempData_EvenIfAccessed();
        }

        [Fact]
        public Task Peek_RetainsTempData()
        {
            return _tempDataCommonTest.Peek_RetainsTempData();
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/21
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public Task ValidTypes_RoundTripProperly()
        {
            return _tempDataCommonTest.ValidTypes_RoundTripProperly();
        }

        [Fact]
        public Task SetInActionResultExecution_AvailableForNextRequest()
        {
            return _tempDataCommonTest.SetInActionResultExecution_AvailableForNextRequest();
        }
    }
}