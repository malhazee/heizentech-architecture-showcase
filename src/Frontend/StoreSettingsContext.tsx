"use client";

import React, { createContext, useContext, useState, useEffect } from "react";
import { fetchApi } from "./api-client";

export interface StoreSettings {
  storeName: string;
  storeTitle: string;
  logoUrl: string;
  faviconUrl: string;
  primaryColor: string;
  currency: string;
  isMaintenanceMode: boolean;
}

const defaultSettings: StoreSettings = {
  storeName: "Heizentech Store",
  storeTitle: "متجر سحابي متكامل",
  logoUrl: "/assests/heizentech-logo.png",
  faviconUrl: "/favicon.ico",
  primaryColor: "#0d9488",
  currency: "JOD",
  isMaintenanceMode: false,
};

interface StoreSettingsContextType {
  settings: StoreSettings;
  isLoading: boolean;
  refreshSettings: () => Promise<void>;
}

const StoreSettingsContext = createContext<StoreSettingsContextType>({
  settings: defaultSettings,
  isLoading: false,
  refreshSettings: async () => {},
});

/**
 * Hydration-safe StoreSettings Provider.
 * Solves React 19 SSR hydration mismatches by initializing with default safe tokens
 * and hydrating client-cached preferences post-mount.
 */
export function StoreSettingsProvider({ children }: { children: React.ReactNode }) {
  const [settings, setSettings] = useState<StoreSettings>(defaultSettings);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [hydrated, setHydrated] = useState<boolean>(false);

  useEffect(() => {
    // 1. Read cached localStorage settings on client mount (avoids React #418 error)
    try {
      const cached = localStorage.getItem("ht_store_settings");
      if (cached) {
        setSettings(JSON.parse(cached));
      }
    } catch {
      // Fallback gracefully
    } finally {
      setHydrated(true);
    }
  }, []);

  const loadSettings = async () => {
    try {
      setIsLoading(true);
      const data = await fetchApi<StoreSettings>("/storesettings");
      if (data && data.storeName) {
        setSettings(data);
        localStorage.setItem("ht_store_settings", JSON.stringify(data));
      }
    } catch (err) {
      console.warn("Failed to fetch tenant store settings, falling back to defaults.", err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (hydrated) {
      loadSettings();
    }
  }, [hydrated]);

  return (
    <StoreSettingsContext.Provider value={{ settings, isLoading, refreshSettings: loadSettings }}>
      {children}
    </StoreSettingsContext.Provider>
  );
}

export const useStoreSettings = () => useContext(StoreSettingsContext);
