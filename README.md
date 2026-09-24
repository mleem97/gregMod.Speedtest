# gregMod.Speedtest

In-game speedtest (speedtest.net style) measuring **real data streams**:
aimed-server egress (`currentProcessingSpeed`) and link speeds
(`connectionSpeed`) sampled over 5 s.

- **DOWN**: link bottleneck into the server (game units)
- **UP**: processing egress (game units)
- **JITTER**: deviation of down samples

No invented metrics — everything is sampled live. Toolkit panel (F4),
F1-hub entry, completion toast, run history. Hard dependency on gregCore.
