/**
 * Universal Multi-Tenant API Client for Next.js 16 (Client & Server Components).
 * Automatically resolves tenant subdomains from browser host or forwarded headers.
 */

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "https://api.heizentech.com/api";

export async function fetchApi<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const url = endpoint.startsWith("http") ? endpoint : `${API_BASE_URL}${endpoint.startsWith("/") ? "" : "/"}${endpoint}`;

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string>),
  };

  // Automatically attach tenant subdomain if in browser environment
  if (typeof window !== "undefined") {
    const host = window.location.hostname;
    const parts = host.split(".");
    if (parts.length >= 3 && parts[0] !== "www") {
      headers["X-Tenant-Subdomain"] = parts[0];
    }

    // Attach JWT if available
    const token = localStorage.getItem("token") || localStorage.getItem("ht_customer_token");
    if (token) {
      headers["Authorization"] = `Bearer ${token}`;
    }
  }

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const errorData = await response.json().catch(() => null);
    throw new Error(errorData?.message || `HTTP error! status: ${response.status}`);
  }

  return response.json();
}
