// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class SaveTempDataFilterTest
    {
        public static TheoryData<IActionResult> ResultData
        {
            get
            {
                return new TheoryData<IActionResult>()
                {
                    new ContentResult() { Content = "Blah" }, // does NOT implement IKeepTempDataResult
                    new RedirectToActionResult("index", "home", routeValues: null) // implements IKeepTempDataResult
                };
            }
        }

        [Theory]
        [MemberData(nameof(ResultData))]
        public void SaveTempDataFilter_AlwaysSavesTempData_OnResultExecuting(IActionResult result)
        {
            // Arrange
            var tempData = new Mock<ITempDataDictionary>(MockBehavior.Strict);
            tempData
                .Setup(m => m.Keep());
            tempData
                .Setup(m => m.Save())
                .Verifiable();

            var tempDataFactory = new Mock<ITempDataDictionaryFactory>(MockBehavior.Strict);
            tempDataFactory
                .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
                .Returns(tempData.Object);

            var filter = new SaveTempDataFilter(tempDataFactory.Object);

            var context = new ResultExecutingContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new IFilterMetadata[] { },
                result,
                new TestController());

            // Act
            filter.OnResultExecuting(context);

            // Assert
            tempData.Verify();
        }

        [Fact]
        public void SaveTempDataFilter_OnResultExecuting_KeepsTempData_ForIKeepTempDataResult()
        {
            // Arrange
            var tempData = new Mock<ITempDataDictionary>(MockBehavior.Strict);
            tempData
                .Setup(m => m.Keep())
                .Verifiable();
            tempData
                .Setup(m => m.Save());

            var tempDataFactory = new Mock<ITempDataDictionaryFactory>(MockBehavior.Strict);
            tempDataFactory
                .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
                .Returns(tempData.Object);

            var filter = new SaveTempDataFilter(tempDataFactory.Object);

            var context = new ResultExecutingContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new IFilterMetadata[] { },
                new Mock<IKeepTempDataResult>().Object,
                new TestController());

            // Act
            filter.OnResultExecuting(context);

            // Assert
            tempData.Verify();
        }

        [Fact]
        public void SaveTempDataFilter_OnResultExecuting_DoesNotKeepTempData_ForNonIKeepTempDataResult()
        {
            // Arrange
            var tempData = new Mock<ITempDataDictionary>(MockBehavior.Strict);
            tempData
                .Setup(m => m.Save());

            var tempDataFactory = new Mock<ITempDataDictionaryFactory>(MockBehavior.Strict);
            tempDataFactory
                .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
                .Returns(tempData.Object);

            var filter = new SaveTempDataFilter(tempDataFactory.Object);

            var context = new ResultExecutingContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new IFilterMetadata[] { },
                new Mock<IActionResult>().Object,
                new TestController());

            // Act
            filter.OnResultExecuting(context);

            // Assert - The mock will throw if we do the wrong thing. i.e since mock behavior is Strict.
        }

        private class TestController : Controller
        {
        }
    }
}