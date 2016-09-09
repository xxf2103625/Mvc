// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    /// <summary>
    /// A filter that saves temp data.
    /// </summary>
    public class SaveTempDataFilter : IResourceFilter, IResultFilter
    {
        private readonly ITempDataDictionaryFactory _factory;

        /// <summary>
        /// Creates a new instance of <see cref="SaveTempDataFilter"/>.
        /// </summary>
        /// <param name="factory">The <see cref="ITempDataDictionaryFactory"/>.</param>
        public SaveTempDataFilter(ITempDataDictionaryFactory factory)
        {
            _factory = factory;
        }

        /// <inheritdoc />
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
        }

        /// <inheritdoc />
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }

        /// <inheritdoc />
        public void OnResultExecuting(ResultExecutingContext context)
        {
            context.HttpContext.Response.OnStarting((state) =>
            {
                var saveTempDataContext = (SaveTempDataContext)state;

                // If temp data was already saved, skip trying to save again as the calls here would potentially fail
                // because the session feature might not be available at this point.
                // Example: An action returns NoContentResult and since NoContentResult does not write anything to
                // the body of the response, this delegate would get executed way late in the pipeline at which point
                // the session feature would have been removed.
                object obj;
                if (saveTempDataContext.HttpContext.Items.TryGetValue(typeof(SaveTempDataContext), out obj))
                {
                    return TaskCache.CompletedTask;
                }

                SaveTempData(
                    saveTempDataContext.ActionResult,
                    saveTempDataContext.TempDataDictionaryFactory,
                    saveTempDataContext.HttpContext);

                return TaskCache.CompletedTask;
            },
            state: new SaveTempDataContext()
            {
                HttpContext = context.HttpContext,
                ActionResult = context.Result,
                TempDataDictionaryFactory = _factory
            });
        }

        /// <inheritdoc />
        public void OnResultExecuted(ResultExecutedContext context)
        {
            // We are doing this here again because the OnStarting delegate above might get fired too late in scenarios
            // where the action result doesn't write anything to the body. This causes the delegate to be executed
            // late in the pipeline at which point SessionFeature would not be available.
            if (!context.HttpContext.Response.HasStarted)
            {
                SaveTempData(context.Result, _factory, context.HttpContext);
                context.HttpContext.Items.Add(typeof(SaveTempDataContext), true);
            }
        }

        private static void SaveTempData(IActionResult result, ITempDataDictionaryFactory factory, HttpContext httpContext)
        {
            if (result is IKeepTempDataResult)
            {
                factory.GetTempData(httpContext).Keep();
            }
            factory.GetTempData(httpContext).Save();
        }

        private class SaveTempDataContext
        {
            public HttpContext HttpContext { get; set; }
            public IActionResult ActionResult { get; set; }
            public ITempDataDictionaryFactory TempDataDictionaryFactory { get; set; }
        }
    }
}
