import requests
import json
import os
import time

def get_releases(repo):
    url = f"https://api.github.com/repos/{repo}/releases"
    response = requests.get(url)
    response.raise_for_status()
    return response.json()

def get_latest_and_latest_pre_release(releases):
    latest_release = None
    latest_pre_release = None

    for release in releases:
        if not release["draft"]:  # Ignore draft releases
            if release["prerelease"]:
                if latest_pre_release is None:
                    latest_pre_release = release
            else:
                if latest_release is None:
                    latest_release = release
            if latest_release and latest_pre_release:
                break

    return latest_release, latest_pre_release

def fetch_manifest(repo):
    url = f"https://raw.githubusercontent.com/{repo}/main/manifest.json"
    try:
        response = requests.get(url)
        response.raise_for_status()
        content = response.content.decode('utf-8-sig')
        return json.loads(content)
    except requests.RequestException as e:
        print(f"Failed to fetch manifest for {repo}: {e}")
        return None

def append_changelog(manifest, latest_release):
    if latest_release:
        manifest["Changelog"] = latest_release.get("body", "No changelog available")
    return manifest

def append_manifest(manifest, latest_release, latest_pre_release):
    # Append details for the latest release only if it has assets (download links)
    if latest_release and latest_release.get("assets"):
        manifest["DownloadLinkInstall"] = latest_release["assets"][0]["browser_download_url"]
        manifest["AssemblyVersion"] = latest_release.get("tag_name", "")
        manifest["DownloadLinkUpdate"] = latest_release["assets"][0]["browser_download_url"]
    else:
        print(f"No assets found in the latest release. Retaining existing download links and assembly version for {manifest.get('AssemblyVersion', 'Unknown')}.")

    # Append details for the latest pre-release if it has assets (download links)
    if latest_pre_release and latest_pre_release.get("assets"):
        manifest["DownloadLinkTesting"] = latest_pre_release["assets"][0]["browser_download_url"]
        manifest["TestingAssemblyVersion"] = latest_pre_release.get("tag_name", "")
    else:
        print(f"No assets found in the latest pre-release. Retaining existing download links for testing.")

    # If no stable release but pre-release is available, set pre-release as main
    if latest_release is None and latest_pre_release:
        manifest["DownloadLinkInstall"] = manifest.get("DownloadLinkTesting", manifest.get("DownloadLinkInstall"))
        manifest["AssemblyVersion"] = manifest.get("TestingAssemblyVersion", manifest.get("AssemblyVersion"))

    return manifest

def append_download_count(manifest, releases):
    download_count = sum(asset["download_count"] for release in releases for asset in release.get("assets", []))
    manifest["DownloadCount"] = download_count
    return manifest

def main():
    with open('repos.txt', 'r') as f:
        repos = f.read().splitlines()

    combined_manifests = []
    
    for repo in repos:
        try:
            releases = get_releases(repo)
        except requests.RequestException as e:
            print(f"Error fetching releases for {repo}: {e}")
            continue

        latest_release, latest_pre_release = get_latest_and_latest_pre_release(releases)
        
        if (latest_pre_release is None and latest_release is not None) or (
                latest_release is not None and latest_pre_release is not None and latest_release["tag_name"] == latest_pre_release["tag_name"]):
            latest_pre_release = None
        
        if latest_release is None and latest_pre_release is None:
            print(f"Skipping {repo} as there are no releases")
            continue

        manifest = fetch_manifest(repo)
        if manifest is None:
            print(f"Skipping {repo} due to missing or invalid manifest.")
            continue

        # Append only if there are valid releases and pre-releases
        manifest = append_manifest(manifest, latest_release, latest_pre_release)
        manifest = append_download_count(manifest, releases)
        manifest = append_changelog(manifest, latest_release)
        
        combined_manifests.append(manifest)

        print(f"{repo}: {latest_release['tag_name'] if latest_release else 'Testing Only'} {latest_pre_release['tag_name'] if latest_pre_release else ''}")

    with open('pluginmaster.json', 'w') as f:
        json.dump(combined_manifests, f, indent=4)

if __name__ == "__main__":
    main()
