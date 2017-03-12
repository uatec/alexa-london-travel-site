﻿// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace MartinCostello.LondonTravel.Site.Tfl
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using MartinCostello.LondonTravel.Site.Options;
    using Microsoft.Extensions.Caching.Memory;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A class representing the default implementation of <see cref="ITflService"/>.
    /// </summary>
    public sealed class TflService : ITflService, IDisposable
    {
        /// <summary>
        /// The <see cref="HttpClient"/> to use. This field is read-only.
        /// </summary>
        private readonly HttpClient _client;

        /// <summary>
        /// The <see cref="IMemoryCache"/> to use. This field is read-only.
        /// </summary>
        private readonly IMemoryCache _cache;

        /// <summary>
        /// The <see cref="TflOptions"/> to use. This field is read-only.
        /// </summary>
        private readonly TflOptions _options;

        /// <summary>
        /// Whether the instance has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TflService"/> class.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> to use.</param>
        /// <param name="cache">The <see cref="IMemoryCache"/> to use.</param>
        /// <param name="options">The <see cref="TflOptions"/> to use.</param>
        public TflService(HttpClient httpClient, IMemoryCache cache, TflOptions options)
        {
            _client = httpClient;
            _cache = cache;
            _options = options;

            _client.BaseAddress = _options.BaseUri;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _client?.Dispose();
                _disposed = true;
            }
        }

        /// <inheritdoc />
        public async Task<JArray> GetLinesAsync(CancellationToken cancellationToken)
        {
            const string CacheKey = "TfL.AvailableLines";

            if (!_cache.TryGetValue(CacheKey, out JArray lines))
            {
                string requestUrl = $"Line/Mode/{string.Join(",", _options.SupportedModes)}?app_id={_options.AppId}&app_key={_options.AppKey}";

                using (var response = await _client.GetAsync(requestUrl, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();

                    lines = JArray.Parse(await response.Content.ReadAsStringAsync());

                    if (response.Headers.CacheControl.MaxAge.HasValue)
                    {
                        var options = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(response.Headers.CacheControl.MaxAge.Value);

                        _cache.Set(CacheKey, lines, options);
                    }
                }
            }

            return lines;
        }
    }
}
