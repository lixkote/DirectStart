// ==WindhawkMod==
// @id              startintercept
// @name            Start Button Intercept for DirectStart
// @description     Intercepts CImmersiveLauncher::ShowStartView, so that DirectStart gets opened instead of Windows 8's start screen.
// @version         1.0
// @author          lixkote
// @github          https://github.com/lixkote/DirectStart
// @include         explorer.exe
// @compilerOptions -lole32
// ==/WindhawkMod==

// ==WindhawkModReadme==
/*
# Start Button Intercept
Intercepts CImmersiveLauncher::ShowStartView, so that DirectStart gets opened instead of Windows 8's start screen.
Only for Windows 8.1.
*/
// ==/WindhawkModReadme==

#include <windhawk_utils.h>
#include <windows.h>

#ifdef _WIN64
#define STDCALL  __cdecl
#define SSTDCALL L"__cdecl"
#else
#define STDCALL  __thiscall
#define SSTDCALL L"__thiscall"
#endif
#define WM_START_TRIGGERED (WM_APP + 1)

enum IMMERSIVELAUNCHERSHOWMETHOD
{
    ILSM_STARTCHARM = 2,
    ILSM_SEARCHPANE = 3,
    ILSM_MOUSECORNER = 5,
    ILSM_APPCRASHED = 7,
    ILSM_COMMANDBAR = 8,
    ILSM_MONITORDISCONNECT = 9,
    ILSM_STARTPERSONALIZE = 10,
};

enum IMMERSIVELAUNCHERSHOWFLAGS
{
    ILSF_DO_NOT_SET_FOREGROUND = 0x1,
    ILSF_PRESERVE_EDIT_MODE = 0x2,
};

#pragma region common
typedef HRESULT (STDCALL *pOriginalShowStart_t)(IMMERSIVELAUNCHERSHOWMETHOD, IMMERSIVELAUNCHERSHOWFLAGS);
pOriginalShowStart_t pOriginalShowStart;
HRESULT STDCALL ShowStartHook(
    const IMMERSIVELAUNCHERSHOWMETHOD method, const IMMERSIVELAUNCHERSHOWFLAGS flags
)
{
    HRESULT hr = S_OK;
    HWND hwnds = FindWindowW(NULL, L"StartMenu");
    if (hwnds)
    {
        SendMessageW(hwnds, WM_START_TRIGGERED, 0, 0);
    }
    return hr;
}
#pragma endregion

BOOL Wh_ModInit() {
    Wh_Log(L"Init");

    WindhawkUtils::SYMBOL_HOOK hooks[] = {
    {
        {L"public: virtual long " SSTDCALL " CImmersiveLauncher::ShowStartView(enum IMMERSIVELAUNCHERSHOWMETHOD,enum IMMERSIVELAUNCHERSHOWFLAGS)"},
        &pOriginalShowStart,
        ShowStartHook,
        false
    }};
    HMODULE hMod = LoadLibraryEx(L"twinui.dll", NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (!WindhawkUtils::HookSymbols(hMod, hooks, 1))
    {
        Wh_Log(L"Failed to hook ShowStartView from %s", L"twinui.dll");
        return FALSE;
    }
    else
    {
        Wh_Log(L"Hooked CImmersiveLauncher::ShowStartView in twinui.dll");
    }

    return TRUE;
}

void Wh_ModUninit() {
    Wh_Log(L"Uninit");
}
