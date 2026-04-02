package copilot

import (
	"encoding/json"
	"testing"
)

func TestProviderConfig_Headers_JSONRoundTrip(t *testing.T) {
	t.Run("serializes headers", func(t *testing.T) {
		config := ProviderConfig{
			BaseURL: "https://api.example.com",
			APIKey:  "test-key",
			Headers: map[string]string{
				"X-Custom":      "value",
				"Authorization": "Bearer tok",
			},
		}

		data, err := json.Marshal(config)
		if err != nil {
			t.Fatalf("failed to marshal: %v", err)
		}

		var decoded ProviderConfig
		if err := json.Unmarshal(data, &decoded); err != nil {
			t.Fatalf("failed to unmarshal: %v", err)
		}

		if decoded.Headers["X-Custom"] != "value" {
			t.Errorf("expected X-Custom=value, got %q", decoded.Headers["X-Custom"])
		}
		if decoded.Headers["Authorization"] != "Bearer tok" {
			t.Errorf("expected Authorization=Bearer tok, got %q", decoded.Headers["Authorization"])
		}
	})

	t.Run("omits headers when nil", func(t *testing.T) {
		config := ProviderConfig{
			BaseURL: "https://api.example.com",
		}

		data, err := json.Marshal(config)
		if err != nil {
			t.Fatalf("failed to marshal: %v", err)
		}

		jsonStr := string(data)
		if contains(jsonStr, "headers") {
			t.Errorf("expected headers to be omitted, got %s", jsonStr)
		}
	})

	t.Run("omits empty headers with omitempty", func(t *testing.T) {
		config := ProviderConfig{
			BaseURL: "https://api.example.com",
			Headers: map[string]string{},
		}

		data, err := json.Marshal(config)
		if err != nil {
			t.Fatalf("failed to marshal: %v", err)
		}

		jsonStr := string(data)
		// Go's omitempty omits empty maps
		if contains(jsonStr, `"headers"`) {
			t.Errorf("expected empty headers to be omitted with omitempty, got %s", jsonStr)
		}
	})
}

func TestHeaderMergeStrategy_Constants(t *testing.T) {
	tests := []struct {
		name     string
		strategy HeaderMergeStrategy
		expected string
	}{
		{"Override", HeaderMergeStrategyOverride, "override"},
		{"Merge", HeaderMergeStrategyMerge, "merge"},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			if string(tt.strategy) != tt.expected {
				t.Errorf("expected %q, got %q", tt.expected, string(tt.strategy))
			}
		})
	}
}

func TestSessionSendRequest_Headers_JSONSerialization(t *testing.T) {
	t.Run("includes requestHeaders and headerMergeStrategy", func(t *testing.T) {
		req := sessionSendRequest{
			SessionID:           "sess-1",
			Prompt:              "hello",
			RequestHeaders:      map[string]string{"X-Custom": "value", "X-Another": "other"},
			HeaderMergeStrategy: HeaderMergeStrategyMerge,
		}

		data, err := json.Marshal(req)
		if err != nil {
			t.Fatalf("failed to marshal: %v", err)
		}

		var decoded map[string]interface{}
		if err := json.Unmarshal(data, &decoded); err != nil {
			t.Fatalf("failed to unmarshal: %v", err)
		}

		headers, ok := decoded["requestHeaders"].(map[string]interface{})
		if !ok {
			t.Fatal("expected requestHeaders to be a map")
		}
		if headers["X-Custom"] != "value" {
			t.Errorf("expected X-Custom=value, got %v", headers["X-Custom"])
		}
		if headers["X-Another"] != "other" {
			t.Errorf("expected X-Another=other, got %v", headers["X-Another"])
		}
		if decoded["headerMergeStrategy"] != "merge" {
			t.Errorf("expected headerMergeStrategy=merge, got %v", decoded["headerMergeStrategy"])
		}
	})

	t.Run("includes override strategy", func(t *testing.T) {
		req := sessionSendRequest{
			SessionID:           "sess-1",
			Prompt:              "hello",
			RequestHeaders:      map[string]string{"X-Key": "val"},
			HeaderMergeStrategy: HeaderMergeStrategyOverride,
		}

		data, err := json.Marshal(req)
		if err != nil {
			t.Fatalf("failed to marshal: %v", err)
		}

		var decoded map[string]interface{}
		if err := json.Unmarshal(data, &decoded); err != nil {
			t.Fatalf("failed to unmarshal: %v", err)
		}

		if decoded["headerMergeStrategy"] != "override" {
			t.Errorf("expected headerMergeStrategy=override, got %v", decoded["headerMergeStrategy"])
		}
	})

	t.Run("omits requestHeaders when nil", func(t *testing.T) {
		req := sessionSendRequest{
			SessionID: "sess-1",
			Prompt:    "hello",
		}

		data, err := json.Marshal(req)
		if err != nil {
			t.Fatalf("failed to marshal: %v", err)
		}

		jsonStr := string(data)
		if contains(jsonStr, "requestHeaders") {
			t.Errorf("expected requestHeaders to be omitted, got %s", jsonStr)
		}
		if contains(jsonStr, "headerMergeStrategy") {
			t.Errorf("expected headerMergeStrategy to be omitted, got %s", jsonStr)
		}
	})

	t.Run("roundtrip with headers preserves values", func(t *testing.T) {
		original := sessionSendRequest{
			SessionID:           "sess-1",
			Prompt:              "test",
			RequestHeaders:      map[string]string{"Authorization": "Bearer token123"},
			HeaderMergeStrategy: HeaderMergeStrategyOverride,
		}

		data, err := json.Marshal(original)
		if err != nil {
			t.Fatalf("failed to marshal: %v", err)
		}

		var decoded sessionSendRequest
		if err := json.Unmarshal(data, &decoded); err != nil {
			t.Fatalf("failed to unmarshal: %v", err)
		}

		if decoded.RequestHeaders["Authorization"] != "Bearer token123" {
			t.Errorf("expected Authorization=Bearer token123, got %q", decoded.RequestHeaders["Authorization"])
		}
		if decoded.HeaderMergeStrategy != HeaderMergeStrategyOverride {
			t.Errorf("expected strategy override, got %q", decoded.HeaderMergeStrategy)
		}
	})

	t.Run("omits empty requestHeaders with omitempty", func(t *testing.T) {
		req := sessionSendRequest{
			SessionID:      "sess-1",
			Prompt:         "hello",
			RequestHeaders: map[string]string{},
		}

		data, err := json.Marshal(req)
		if err != nil {
			t.Fatalf("failed to marshal: %v", err)
		}

		jsonStr := string(data)
		// Go's omitempty omits empty maps
		if contains(jsonStr, `"requestHeaders"`) {
			t.Errorf("expected empty requestHeaders to be omitted with omitempty, got %s", jsonStr)
		}
	})
}

func TestMessageOptions_Headers(t *testing.T) {
	t.Run("can set request headers and merge strategy", func(t *testing.T) {
		opts := MessageOptions{
			Prompt:              "test",
			RequestHeaders:      map[string]string{"X-Key": "val"},
			HeaderMergeStrategy: HeaderMergeStrategyMerge,
		}

		if opts.RequestHeaders["X-Key"] != "val" {
			t.Errorf("expected X-Key=val, got %q", opts.RequestHeaders["X-Key"])
		}
		if opts.HeaderMergeStrategy != HeaderMergeStrategyMerge {
			t.Errorf("expected merge strategy, got %q", opts.HeaderMergeStrategy)
		}
	})

	t.Run("defaults to empty values", func(t *testing.T) {
		opts := MessageOptions{
			Prompt: "test",
		}

		if opts.RequestHeaders != nil {
			t.Errorf("expected nil RequestHeaders, got %v", opts.RequestHeaders)
		}
		if opts.HeaderMergeStrategy != "" {
			t.Errorf("expected empty HeaderMergeStrategy, got %q", opts.HeaderMergeStrategy)
		}
	})
}

// contains checks if substr is present in s.
func contains(s, substr string) bool {
	return len(s) >= len(substr) && searchString(s, substr)
}

func searchString(s, substr string) bool {
	for i := 0; i <= len(s)-len(substr); i++ {
		if s[i:i+len(substr)] == substr {
			return true
		}
	}
	return false
}
