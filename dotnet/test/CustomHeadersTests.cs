/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using Xunit;

namespace GitHub.Copilot.SDK.Test;

/// <summary>
/// Unit tests for custom headers support in MessageOptions, ProviderConfig,
/// HeaderMergeStrategy, and related serialization.
/// </summary>
public class CustomHeadersTests
{
    [Fact]
    public void ProviderConfig_Headers_CanBeSet()
    {
        var config = new ProviderConfig
        {
            BaseUrl = "https://api.example.com",
            ApiKey = "test-key",
            Headers = new Dictionary<string, string>
            {
                ["X-Custom"] = "value",
                ["Authorization"] = "Bearer tok",
            },
        };

        Assert.Equal("value", config.Headers["X-Custom"]);
        Assert.Equal("Bearer tok", config.Headers["Authorization"]);
    }

    [Fact]
    public void ProviderConfig_Headers_NullByDefault()
    {
        var config = new ProviderConfig();
        Assert.Null(config.Headers);
    }

    [Fact]
    public void ProviderConfig_Headers_CanBeEmpty()
    {
        var config = new ProviderConfig
        {
            Headers = new Dictionary<string, string>(),
        };

        Assert.NotNull(config.Headers);
        Assert.Empty(config.Headers);
    }

    [Fact]
    public void HeaderMergeStrategy_HasExpectedValues()
    {
        Assert.Equal("override", HeaderMergeStrategy.Override);
        Assert.Equal("merge", HeaderMergeStrategy.Merge);
    }

    [Fact]
    public void MessageOptions_RequestHeaders_CanBeSet()
    {
        var options = new MessageOptions
        {
            Prompt = "test",
            RequestHeaders = new Dictionary<string, string>
            {
                ["X-Custom"] = "value",
                ["X-Another"] = "other",
            },
        };

        Assert.Equal("value", options.RequestHeaders["X-Custom"]);
        Assert.Equal("other", options.RequestHeaders["X-Another"]);
    }

    [Fact]
    public void MessageOptions_HeaderMergeStrategy_Override()
    {
        var options = new MessageOptions
        {
            Prompt = "test",
            HeaderMergeStrategy = HeaderMergeStrategy.Override,
        };

        Assert.Equal("override", options.HeaderMergeStrategy);
    }

    [Fact]
    public void MessageOptions_HeaderMergeStrategy_Merge()
    {
        var options = new MessageOptions
        {
            Prompt = "test",
            HeaderMergeStrategy = HeaderMergeStrategy.Merge,
        };

        Assert.Equal("merge", options.HeaderMergeStrategy);
    }

    [Fact]
    public void MessageOptions_RequestHeaders_NullByDefault()
    {
        var options = new MessageOptions { Prompt = "test" };
        Assert.Null(options.RequestHeaders);
        Assert.Null(options.HeaderMergeStrategy);
    }

    [Fact]
    public void MessageOptions_Clone_CopiesHeaders()
    {
        var original = new MessageOptions
        {
            Prompt = "test",
            RequestHeaders = new Dictionary<string, string>
            {
                ["X-Custom"] = "value",
            },
            HeaderMergeStrategy = HeaderMergeStrategy.Merge,
        };

        var clone = original.Clone();

        Assert.Equal(original.Prompt, clone.Prompt);
        Assert.Equal(original.HeaderMergeStrategy, clone.HeaderMergeStrategy);
        Assert.NotNull(clone.RequestHeaders);
        Assert.Equal("value", clone.RequestHeaders["X-Custom"]);
    }

    [Fact]
    public void MessageOptions_Clone_HeadersAreIndependent()
    {
        var original = new MessageOptions
        {
            Prompt = "test",
            RequestHeaders = new Dictionary<string, string>
            {
                ["X-Custom"] = "value",
            },
        };

        var clone = original.Clone();

        clone.RequestHeaders!["X-New"] = "added";

        Assert.False(original.RequestHeaders.ContainsKey("X-New"));
        Assert.Single(original.RequestHeaders);
    }

    [Fact]
    public void MessageOptions_Clone_NullHeaders_StayNull()
    {
        var original = new MessageOptions
        {
            Prompt = "test",
        };

        var clone = original.Clone();

        Assert.Null(clone.RequestHeaders);
        Assert.Null(clone.HeaderMergeStrategy);
    }

    [Fact]
    public void MessageOptions_WithAllHeaderFields()
    {
        var options = new MessageOptions
        {
            Prompt = "hello",
            RequestHeaders = new Dictionary<string, string>
            {
                ["X-Request-Id"] = "req-123",
            },
            HeaderMergeStrategy = HeaderMergeStrategy.Merge,
            Mode = "enqueue",
        };

        Assert.Equal("hello", options.Prompt);
        Assert.Equal("req-123", options.RequestHeaders["X-Request-Id"]);
        Assert.Equal("merge", options.HeaderMergeStrategy);
        Assert.Equal("enqueue", options.Mode);
    }
}
