﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class CookieTempDataProviderTest
    {
        [Fact]
        public void LoadTempData_ReturnsEmptyDictionary_WhenNoCookieDataIsAvailable()
        {
            // Arrange
            var tempDataProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(new PassThroughDataProtector()));

            // Act
            var tempDataDictionary = tempDataProvider.LoadTempData(new DefaultHttpContext());

            // Assert
            Assert.Empty(tempDataDictionary);
        }

        [Fact]
        public void LoadTempData_Base64DecodesAnd_UnprotectsData_FromCookie()
        {
            // Arrange
            var expectedValues = new Dictionary<string, object>();
            expectedValues.Add("int", 10);
            var tempDataProviderSerializer = new TempDataSerializer();
            var expectedDataToUnprotect = tempDataProviderSerializer.SerializeTempData(expectedValues);
            var base64EncodedDataInCookie = Convert.ToBase64String(expectedDataToUnprotect);
            var dataProtector = new PassThroughDataProtector();
            var tempDataProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(dataProtector));
            var requestCookies = new RequestCookieCollection(new Dictionary<string, string>()
            {
                { CookieTempDataProvider.CookieName, base64EncodedDataInCookie }
            });
            var httpContext = new Mock<HttpContext>();
            httpContext
               .Setup(hc => hc.Request.Cookies)
               .Returns(requestCookies);

            // Act
            var actualValues = tempDataProvider.LoadTempData(httpContext.Object);

            // Assert
            Assert.Equal(expectedDataToUnprotect, dataProtector.DataToUnprotect);
            Assert.Equal(expectedValues, actualValues);
        }

        [Fact]
        public void SaveTempData_ProtectsAnd_Base64EncodesDataAnd_SetsCookie()
        {
            // Arrange
            var values = new Dictionary<string, object>();
            values.Add("int", 10);
            var tempDataProviderStore = new TempDataSerializer();
            var expectedDataToProtect = tempDataProviderStore.SerializeTempData(values);
            var expectedDataInCookie = Convert.ToBase64String(expectedDataToProtect);
            var dataProtector = new PassThroughDataProtector();
            var tempDataProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(dataProtector));
            var responseCookies = new MockResponseCookieCollection();
            var httpContext = new Mock<HttpContext>();
            httpContext
                .SetupGet(hc => hc.Request.PathBase)
                .Returns("/");
            httpContext
                .Setup(hc => hc.Response.Cookies)
                .Returns(responseCookies);

            // Act
            tempDataProvider.SaveTempData(httpContext.Object, values);

            // Assert
            Assert.Equal(1, responseCookies.Count);
            var cookieInfo = responseCookies[CookieTempDataProvider.CookieName];
            Assert.NotNull(cookieInfo);
            Assert.Equal(expectedDataInCookie, cookieInfo.Value);
            Assert.Equal(expectedDataToProtect, dataProtector.PlainTextToProtect);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/vdir1")]
        public void SaveTempData_SetsCookie_WithAppropriateCookieOptions(string pathBase)
        {
            // Arrange
            var values = new Dictionary<string, object>();
            values.Add("int", 10);
            var tempDataProviderStore = new TempDataSerializer();
            var expectedDataToProtect = tempDataProviderStore.SerializeTempData(values);
            var expectedDataInCookie = Convert.ToBase64String(expectedDataToProtect);
            var dataProtector = new PassThroughDataProtector();
            var tempDataProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(dataProtector));
            var responseCookies = new MockResponseCookieCollection();
            var httpContext = new Mock<HttpContext>();
            httpContext
                .SetupGet(hc => hc.Request.PathBase)
                .Returns(pathBase);
            httpContext
                .Setup(hc => hc.Response.Cookies)
                .Returns(responseCookies);

            // Act
            tempDataProvider.SaveTempData(httpContext.Object, values);

            // Assert
            Assert.Equal(1, responseCookies.Count);
            var cookieInfo = responseCookies[CookieTempDataProvider.CookieName];
            Assert.NotNull(cookieInfo);
            Assert.Equal(expectedDataInCookie, cookieInfo.Value);
            Assert.Equal(expectedDataToProtect, dataProtector.PlainTextToProtect);
            Assert.Equal(pathBase, cookieInfo.Options.Path);
            Assert.True(cookieInfo.Options.Secure);
            Assert.True(cookieInfo.Options.HttpOnly);
            Assert.Null(cookieInfo.Options.Expires);
            Assert.Null(cookieInfo.Options.Domain);
        }

        [Fact]
        public void SaveTempData_RemovesCookie_WhenNoDataToSave()
        {
            // Arrange
            var values = new Dictionary<string, object>();
            values.Add("int", 10);
            var tempDataProviderStore = new TempDataSerializer();
            var serializedData = tempDataProviderStore.SerializeTempData(values);
            var base64EncodedData = Convert.ToBase64String(serializedData);
            var dataProtector = new PassThroughDataProtector();
            var tempDataProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(dataProtector));
            var requestCookies = new RequestCookieCollection(new Dictionary<string, string>()
            {
                { CookieTempDataProvider.CookieName, base64EncodedData }
            });
            var responseCookies = new MockResponseCookieCollection();
            var httpContext = new Mock<HttpContext>();
            httpContext
                .SetupGet(hc => hc.Request.PathBase)
                .Returns("/");
            httpContext
                .Setup(hc => hc.Request.Cookies)
                .Returns(requestCookies);
            httpContext
                .Setup(hc => hc.Response.Cookies)
                .Returns(responseCookies);

            // Act
            tempDataProvider.SaveTempData(httpContext.Object, new Dictionary<string, object>());

            // Assert
            Assert.Equal(0, responseCookies.Count);
        }

        [Fact]
        public void SaveAndLoad_StringCanBeStoredAndLoaded()
        {
            // Arrange
            var testProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(new PassThroughDataProtector()));
            var input = new Dictionary<string, object>
            {
                { "string", "value" }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            UpdateRequestWithCookies(context);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var stringVal = Assert.IsType<string>(TempData["string"]);
            Assert.Equal("value", stringVal);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void SaveAndLoad_IntCanBeStoredAndLoaded(int expected)
        {
            // Arrange
            var testProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(new PassThroughDataProtector()));
            var input = new Dictionary<string, object>
            {
                { "int", expected }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            UpdateRequestWithCookies(context);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var intVal = Assert.IsType<int>(TempData["int"]);
            Assert.Equal(expected, intVal);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SaveAndLoad_BoolCanBeStoredAndLoaded(bool value)
        {
            // Arrange
            var testProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(new PassThroughDataProtector()));
            var input = new Dictionary<string, object>
            {
                { "bool", value }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            UpdateRequestWithCookies(context);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var boolVal = Assert.IsType<bool>(TempData["bool"]);
            Assert.Equal(value, boolVal);
        }

        [Fact]
        public void SaveAndLoad_DateTimeCanBeStoredAndLoaded()
        {
            // Arrange
            var testProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(new PassThroughDataProtector()));
            var inputDatetime = new DateTime(2010, 12, 12, 1, 2, 3, DateTimeKind.Local);
            var input = new Dictionary<string, object>
            {
                { "DateTime", inputDatetime }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            UpdateRequestWithCookies(context);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var datetime = Assert.IsType<DateTime>(TempData["DateTime"]);
            Assert.Equal(inputDatetime, datetime);
        }

        [Fact]
        public void SaveAndLoad_GuidCanBeStoredAndLoaded()
        {
            // Arrange
            var testProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(new PassThroughDataProtector()));
            var inputGuid = Guid.NewGuid();
            var input = new Dictionary<string, object>
            {
                { "Guid", inputGuid }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            UpdateRequestWithCookies(context);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var guidVal = Assert.IsType<Guid>(TempData["Guid"]);
            Assert.Equal(inputGuid, guidVal);
        }

        [Fact]
        public void SaveAndLoad_EnumCanBeSavedAndLoaded()
        {
            // Arrange
            var key = "EnumValue";
            var testProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(new PassThroughDataProtector()));
            var expected = DayOfWeek.Friday;
            var input = new Dictionary<string, object>
            {
                { key, expected }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            UpdateRequestWithCookies(context);
            var TempData = testProvider.LoadTempData(context);
            var result = TempData[key];

            // Assert
            var actual = (DayOfWeek)result;
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(3100000000L)]
        [InlineData(-3100000000L)]
        public void SaveAndLoad_LongCanBeSavedAndLoaded(long expected)
        {
            // Arrange
            var key = "LongValue";
            var testProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(new PassThroughDataProtector()));
            var input = new Dictionary<string, object>
            {
                { key, expected }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            UpdateRequestWithCookies(context);
            var TempData = testProvider.LoadTempData(context);
            var result = TempData[key];

            // Assert
            var actual = Assert.IsType<long>(result);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SaveAndLoad_ListCanBeStoredAndLoaded()
        {
            // Arrange
            var testProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(new PassThroughDataProtector()));
            var input = new Dictionary<string, object>
            {
                { "List`string", new List<string> { "one", "two" } }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            UpdateRequestWithCookies(context);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var list = (IList<string>)TempData["List`string"];
            Assert.Equal(2, list.Count);
            Assert.Equal("one", list[0]);
            Assert.Equal("two", list[1]);
        }

        [Fact]
        public void SaveAndLoad_DictionaryCanBeStoredAndLoaded()
        {
            // Arrange
            var testProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(new PassThroughDataProtector()));
            var inputDictionary = new Dictionary<string, string>
            {
                { "Hello", "World" },
            };
            var input = new Dictionary<string, object>
            {
                { "Dictionary", inputDictionary }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            UpdateRequestWithCookies(context);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var dictionary = Assert.IsType<Dictionary<string, string>>(TempData["Dictionary"]);
            Assert.Equal("World", dictionary["Hello"]);
        }

        [Fact]
        public void SaveAndLoad_EmptyDictionary_RoundTripsAsNull()
        {
            // Arrange
            var testProvider = new CookieTempDataProvider(new PassThroughDataProtectionProvider(new PassThroughDataProtector()));
            var input = new Dictionary<string, object>
            {
                { "EmptyDictionary", new Dictionary<string, int>() }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            UpdateRequestWithCookies(context);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var emptyDictionary = (IDictionary<string, int>)TempData["EmptyDictionary"];
            Assert.Null(emptyDictionary);
        }

        private static HttpContext GetHttpContext()
        {
            var context = new Mock<HttpContext>();
            context
                .SetupGet(hc => hc.Request.PathBase)
                .Returns("/");
            context
                .SetupGet(hc => hc.Response.Cookies)
                .Returns(new MockResponseCookieCollection());
            return context.Object;
        }

        private void UpdateRequestWithCookies(HttpContext httpContext)
        {
            var responseCookies = (MockResponseCookieCollection)httpContext.Response.Cookies;

            var values = new Dictionary<string, string>();

            foreach (var responseCookie in responseCookies)
            {
                values.Add(responseCookie.Key, responseCookie.Value);
            }

            if (values.Count > 0)
            {
                httpContext.Request.Cookies = new RequestCookieCollection(values);
            }
        }

        private class MockResponseCookieCollection : IResponseCookies, IEnumerable<CookieInfo>
        {
            private Dictionary<string, CookieInfo> _cookies = new Dictionary<string, CookieInfo>(StringComparer.OrdinalIgnoreCase);

            public int Count
            {
                get
                {
                    return _cookies.Count;
                }
            }

            public CookieInfo this[string key]
            {
                get
                {
                    return _cookies[key];
                }
            }

            public void Append(string key, string value, CookieOptions options)
            {
                _cookies[key] = new CookieInfo()
                {
                    Key = key,
                    Value = value,
                    Options = options
                };
            }

            public void Append(string key, string value)
            {
                Append(key, value, new CookieOptions());
            }

            public void Delete(string key, CookieOptions options)
            {
                _cookies.Remove(key);
            }

            public void Delete(string key)
            {
                _cookies.Remove(key);
            }

            public IEnumerator<CookieInfo> GetEnumerator()
            {
                return _cookies.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class PassThroughDataProtectionProvider : IDataProtectionProvider
        {
            private readonly IDataProtector _dataProtector;

            public PassThroughDataProtectionProvider(IDataProtector dataProtector)
            {
                _dataProtector = dataProtector;
            }

            public IDataProtector CreateProtector(string purpose)
            {
                return _dataProtector;
            }
        }

        private class PassThroughDataProtector : IDataProtector
        {
            public byte[] DataToUnprotect { get; private set; }
            public byte[] PlainTextToProtect { get; private set; }
            public string Purpose { get; private set; }

            public IDataProtector CreateProtector(string purpose)
            {
                Purpose = purpose;
                return this;
            }

            public byte[] Protect(byte[] plaintext)
            {
                PlainTextToProtect = plaintext;
                return PlainTextToProtect;
            }

            public byte[] Unprotect(byte[] protectedData)
            {
                DataToUnprotect = protectedData;
                return DataToUnprotect;
            }
        }

        private class CookieInfo
        {
            public string Key { get; set; }

            public string Value { get; set; }

            public CookieOptions Options { get; set; }
        }
    }
}