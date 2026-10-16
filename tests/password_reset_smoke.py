"""Run password and API integration checks against a disposable MySQL schema."""
from pathlib import Path
import subprocess


def run():
    project = Path(__file__).resolve().parents[1]
    subprocess.run(
        ["dotnet", "run", "--project", str(project.parent / "tools" / "MySqlSmoke")],
        cwd=project,
        check=True,
    )


if __name__ == "__main__":
    run()
