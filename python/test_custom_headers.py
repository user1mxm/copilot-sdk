"""Tests for custom headers in session.send() and provider configuration."""

from unittest.mock import AsyncMock

import pytest

from copilot.session import CopilotSession, HeaderMergeStrategy, ProviderConfig


def _make_session(client: AsyncMock) -> CopilotSession:
    """Create a CopilotSession with a mocked client for unit testing."""
    return CopilotSession(session_id="sess-1", client=client)


class TestProviderConfigHeaders:
    """Test ProviderConfig TypedDict construction with headers."""

    def test_provider_config_with_headers(self):
        """ProviderConfig can include custom headers."""
        config: ProviderConfig = {
            "base_url": "https://api.example.com",
            "api_key": "test-key",
            "headers": {"X-Custom": "value", "Authorization": "Bearer tok"},
        }
        assert config["headers"]["X-Custom"] == "value"
        assert config["headers"]["Authorization"] == "Bearer tok"

    def test_provider_config_with_empty_headers(self):
        """ProviderConfig can include an empty headers dict."""
        config: ProviderConfig = {
            "base_url": "https://api.example.com",
            "headers": {},
        }
        assert config["headers"] == {}

    def test_provider_config_without_headers(self):
        """ProviderConfig works without the optional headers field."""
        config: ProviderConfig = {
            "base_url": "https://api.example.com",
        }
        assert "headers" not in config


class TestHeaderMergeStrategy:
    """Test HeaderMergeStrategy literal values."""

    def test_override_value(self):
        strategy: HeaderMergeStrategy = "override"
        assert strategy == "override"

    def test_merge_value(self):
        strategy: HeaderMergeStrategy = "merge"
        assert strategy == "merge"


class TestSendWithCustomHeaders:
    """Test that send() passes requestHeaders and headerMergeStrategy to the RPC call."""

    @pytest.mark.asyncio
    async def test_send_includes_request_headers(self):
        """Verify requestHeaders are forwarded in the RPC params."""
        client = AsyncMock()
        client.request = AsyncMock(return_value={"messageId": "msg-1"})

        session = _make_session(client)

        await session.send(
            "test prompt",
            request_headers={"X-Custom": "value", "X-Another": "other"},
        )

        client.request.assert_called_once()
        args, _ = client.request.call_args
        assert args[0] == "session.send"
        params = args[1]
        assert params["requestHeaders"] == {"X-Custom": "value", "X-Another": "other"}

    @pytest.mark.asyncio
    async def test_send_includes_header_merge_strategy_override(self):
        """Verify headerMergeStrategy 'override' is forwarded."""
        client = AsyncMock()
        client.request = AsyncMock(return_value={"messageId": "msg-1"})

        session = _make_session(client)

        await session.send(
            "test",
            request_headers={"X-Key": "val"},
            header_merge_strategy="override",
        )

        args, _ = client.request.call_args
        params = args[1]
        assert params["headerMergeStrategy"] == "override"

    @pytest.mark.asyncio
    async def test_send_includes_header_merge_strategy_merge(self):
        """Verify headerMergeStrategy 'merge' is forwarded."""
        client = AsyncMock()
        client.request = AsyncMock(return_value={"messageId": "msg-1"})

        session = _make_session(client)

        await session.send(
            "test",
            request_headers={"X-Key": "val"},
            header_merge_strategy="merge",
        )

        args, _ = client.request.call_args
        params = args[1]
        assert params["headerMergeStrategy"] == "merge"

    @pytest.mark.asyncio
    async def test_send_omits_headers_when_none(self):
        """Verify requestHeaders and headerMergeStrategy are omitted when not provided."""
        client = AsyncMock()
        client.request = AsyncMock(return_value={"messageId": "msg-1"})

        session = _make_session(client)

        await session.send("test")

        args, _ = client.request.call_args
        params = args[1]
        assert "requestHeaders" not in params
        assert "headerMergeStrategy" not in params

    @pytest.mark.asyncio
    async def test_send_with_empty_request_headers(self):
        """Verify empty requestHeaders dict is forwarded."""
        client = AsyncMock()
        client.request = AsyncMock(return_value={"messageId": "msg-1"})

        session = _make_session(client)

        await session.send("test", request_headers={})

        args, _ = client.request.call_args
        params = args[1]
        assert params["requestHeaders"] == {}

    @pytest.mark.asyncio
    async def test_send_with_both_headers_and_strategy(self):
        """Verify both requestHeaders and headerMergeStrategy are forwarded together."""
        client = AsyncMock()
        client.request = AsyncMock(return_value={"messageId": "msg-1"})

        session = _make_session(client)

        await session.send(
            "hello",
            request_headers={"X-Request-Id": "req-123"},
            header_merge_strategy="merge",
        )

        args, _ = client.request.call_args
        params = args[1]
        assert params["sessionId"] == "sess-1"
        assert params["prompt"] == "hello"
        assert params["requestHeaders"] == {"X-Request-Id": "req-123"}
        assert params["headerMergeStrategy"] == "merge"


class TestUpdateProvider:
    """Test that update_provider() makes the correct RPC call."""

    @pytest.mark.asyncio
    async def test_update_provider_with_headers(self):
        """Verify update_provider sends headers in wire format."""
        from copilot.client import CopilotClient

        client_mock = AsyncMock()
        client_mock.request = AsyncMock(return_value={})

        # Use the real wire format conversion
        client_mock._convert_provider_to_wire_format = (
            CopilotClient._convert_provider_to_wire_format.__get__(client_mock)
        )

        session = _make_session(client_mock)

        await session.update_provider(
            {"headers": {"Authorization": "Bearer token", "X-Custom": "val"}}
        )

        client_mock.request.assert_called_once()
        args, _ = client_mock.request.call_args
        assert args[0] == "session.provider.update"
        params = args[1]
        assert params["sessionId"] == "sess-1"
        assert params["provider"]["headers"] == {
            "Authorization": "Bearer token",
            "X-Custom": "val",
        }

    @pytest.mark.asyncio
    async def test_update_provider_with_empty_headers(self):
        """Verify update_provider with empty headers dict."""
        from copilot.client import CopilotClient

        client_mock = AsyncMock()
        client_mock.request = AsyncMock(return_value={})
        client_mock._convert_provider_to_wire_format = (
            CopilotClient._convert_provider_to_wire_format.__get__(client_mock)
        )

        session = _make_session(client_mock)

        await session.update_provider({"headers": {}})

        args, _ = client_mock.request.call_args
        params = args[1]
        assert params["provider"]["headers"] == {}

    @pytest.mark.asyncio
    async def test_update_provider_wire_format_conversion(self):
        """Verify provider config is converted from snake_case to camelCase."""
        from copilot.client import CopilotClient

        client_mock = AsyncMock()
        client_mock.request = AsyncMock(return_value={})
        client_mock._convert_provider_to_wire_format = (
            CopilotClient._convert_provider_to_wire_format.__get__(client_mock)
        )

        session = _make_session(client_mock)

        await session.update_provider(
            {
                "base_url": "https://api.example.com",
                "api_key": "key-123",
                "headers": {"X-Custom": "value"},
            }
        )

        args, _ = client_mock.request.call_args
        provider = args[1]["provider"]
        assert provider["baseUrl"] == "https://api.example.com"
        assert provider["apiKey"] == "key-123"
        assert provider["headers"] == {"X-Custom": "value"}
        assert "base_url" not in provider
        assert "api_key" not in provider
