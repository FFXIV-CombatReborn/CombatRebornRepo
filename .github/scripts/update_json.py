import requests
import json
import os
import logging
from typing import Tuple, Optional, List, Dict, Any

# Constants
GITHUB_API_URL = "https://api.github.com/repos"
RAW_GITHUB_URL = "https://raw.githubusercontent.com"
BRANCH = "main"
ENCODING = "utf-8-sig"

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

def get_latest_and_latest_pre_release(repo: str) -> Tuple[Optional[Dict[str, Any]], Optional[Dict[str, Any]]]:
    """Fetch the latest release and the latest pre-release from a GitHub repository."""
    url = f"{GITHUB_API_URL}/{repo}/releases"
    response = requests.get(url)
    response.raise_for_status()
    releases = response.json()

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

def fetch_manifest(repo: str) -> Dict[str, Any]:
    """Fetch the manifest.json file from a GitHub repository."""
    url = f"{RAW_GITHUB_URL}/{repo}/{BRANCH}/manifest.json"
    response = requests.get(url)
    response.raise_for_status()
    content = response.content.decode(ENCODING)
    return json.loads(content)

def append_changelog(manifest: Dict[str, Any], latest_release: Optional[Dict[str, Any]]) -> Dict[str, Any]:
    """Append the changelog from the latest release to the manifest."""
    if latest_release:
        manifest["Changelog"] = latest_release["body"]
    return manifest

def append_manifest(manifest: Dict[str, Any], latest_release: Optional[Dict[str, Any]], latest_pre_release: Optional[Dict[str, Any]]) -> Dict[str, Any]:
    """Append release information to the manifest."""
    if latest_release:
        manifest["DownloadLinkInstall"] = latest_release["assets"][0]["browser_download_url"]
        manifest["AssemblyVersion"] = latest_release["tag_name"]
        manifest["DownloadLinkUpdate"] = latest_release["assets"][0]["browser_download_url"]
    if latest_pre_release:
        manifest["DownloadLinkTesting"] = latest_pre_release["assets"][0]["browser_download_url"]
        manifest["TestingAssemblyVersion"] = latest_pre_release["tag_name"]
    if latest_release is None:
        manifest["DownloadLinkInstall"] = manifest["DownloadLinkTesting"]
        manifest["AssemblyVersion"] = manifest["TestingAssemblyVersion"]
    return manifest

def append_download_count(manifest: Dict[str, Any], repo: str) -> Dict[str, Any]:
    """Append the total download count of all releases to the manifest."""
    url = f"{GITHUB_API_URL}/{repo}/releases"
    response = requests.get(url)
    response.raise_for_status()
    releases = response.json()

    download_count = sum(asset["download_count"] for release in releases for asset in release["assets"])
    manifest["DownloadCount"] = download_count
    return manifest

def main():
    """Main function to update the plugin repository JSON."""
    try:
        with open('repos.txt', 'r') as f:
            repos = f.read().splitlines()
    except FileNotFoundError:
        logging.error("repos.txt file not found.")
        return

    combined_manifests = []

    for repo in repos:
        try:
            latest_release, latest_pre_release = get_latest_and_latest_pre_release(repo)
            if (latest_pre_release is None and latest_release is not None) or (latest_release is not None and latest_pre_release is not None and latest_release["tag_name"] == latest_pre_release["tag_name"]):
                latest_pre_release = None
            if latest_release is None and latest_pre_release is None:
                logging.info(f"Skipping {repo} as there are no releases")
                continue
            manifest = fetch_manifest(repo)
            manifest = append_manifest(manifest, latest_release, latest_pre_release)
            manifest = append_download_count(manifest, repo)
            manifest = append_changelog(manifest, latest_release)
            combined_manifests.append(manifest)
            logging.info(f"{repo}: {latest_release['tag_name'] if latest_release else 'Testing Only'} {latest_pre_release['tag_name'] if latest_pre_release else ''}")
        except requests.RequestException as e:
            logging.error(f"Failed to process {repo}: {e}")
            continue

    try:
        with open('pluginmaster.json', 'w') as f:
            json.dump(combined_manifests, f, indent=4)
    except IOError as e:
        logging.error(f"Failed to write pluginmaster.json: {e}")

if __name__ == "__main__":
    main()
