// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class CookieTempDataProvider : ITempDataProvider
    {
        public static readonly string CookieName = ".AspNetCore.Mvc.ViewFeatures.CookieTempDataProvider";
        private static readonly string Purpose = "Microsoft.AspNetCore.Mvc.ViewFeatures.CookieTempDataProviderToken.v1";
        private const byte TokenVersion = 0x01;
        private readonly IDataProtector _dataProtector;
        private TempDataSerializer _tempDataSerializer;

        public CookieTempDataProvider(IDataProtectionProvider dataProtectionProvider)
        {
            _dataProtector = dataProtectionProvider.CreateProtector(Purpose);
            _tempDataSerializer = new TempDataSerializer();
        }

        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            IDictionary<string, object> values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            string base64EncodedValue;
            if (context.Request.Cookies.TryGetValue(CookieName, out base64EncodedValue))
            {
                var protectedData = Convert.FromBase64String(base64EncodedValue);
                var unprotectedData = _dataProtector.Unprotect(protectedData);
                values = _tempDataSerializer.DeserializeTempData(unprotectedData);
            }

            return values;
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var cookieOptions = new CookieOptions()
            {
                Path = context.Request.PathBase,
                HttpOnly = true,
                Secure = true
            };

            var hasValues = (values != null && values.Count > 0);
            if (hasValues)
            {
                var bytes = _tempDataSerializer.SerializeTempData(values);
                bytes = _dataProtector.Protect(bytes);

                context.Response.Cookies.Append(CookieName, Convert.ToBase64String(bytes), cookieOptions);
            }
            else
            {
                context.Response.Cookies.Delete(CookieName, cookieOptions);
            }
        }
    }
}
