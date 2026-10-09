# Reflux

> **Unofficial fork, forked and modified for INFOhSorry (INF오소리).** Use [OhSorryWeb](https://iidx.in) to view and analyze the scores this fork outputs.
> Upstream: https://github.com/olji/Reflux
>
> **INF오소리(INFOhSorry)를 위해 포크해서 수정한 비공식 포크입니다.** 이 포크가 출력한 성적은 [오소리웹](https://iidx.in)에서 보세요.

Probes INFINITAS memory and outputs all top scores into a tsv file.

It also saves plays in sessions files with the following data:
- Settings (RAN,LIFT,Autoscratch etc.)
- Play results (Score, lamp, DJ level)
- Judge data (Pgreat, misses, fast/slow)
- Metadata (Gauge percent, note progress)

It also dumps the database for chart unlocks.

Some live streaming utilities are available as well, such as outputting current playing song and the play state tracking info to separate files for displaying on screen or automating behaviour.

Play data is, depending on configuration, appended to a .tsv session file, a json file and/or sent to a URL running a [Reflux Server](https://github.com/olji/Reflux_Server) (Or homebrew with compatible API). 

Memory offsets used to find the relevant information is stored in a file along with the build version it works with. On start the binary will probe the executable for build version by brute force and compare with what's listed in the offset file.
If no match it will check for an applicable offset file on a configurable server.
This behaviour can be turned off if unwanted.

Full compatibility with Mina's Unlock Tracker Spreadsheet (make your own copy): https://docs.google.com/spreadsheets/d/1oCcBVNUSRKA5NE9d6lYEyUNHPBosMZchtwsy0yL_nSM/edit?usp=sharing
