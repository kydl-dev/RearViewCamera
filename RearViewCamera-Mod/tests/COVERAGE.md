# RearViewCamera - test coverage (spec 12)

Every requirement, and where it is tested. **Automated** = covered by an automated test that ran green; **Manual** = needs a person
(`RearViewCamera-Manual` suite or the release checklist); **Gap** = not covered yet. Never mark an untested behaviour as covered.
Update this file with every feature and every new test.

| Feature / requirement | Unit | Integration | TestLab | Manual | Status |
|---|---|---|---|---|---|
| Loads with its exact KydlCore, no errors | - | ManifestTests, ArchitectureRulesTests | startup-clean | - | Automated |
| KY hub page (F6): registered, tabs, opens / closes | - | - | hub-page | hub-look (theme, real keys) | Automated + Manual |
| Feature switches live (EnableAll / Enable), no stale tabs | KydlCore.Core FeatureRegistry | - | feature-toggle | - | Automated |
| Player config layout (EnableAll, DebugLog, ranges) | - | SettingsTests | (configOverrides) | - | Automated |
| Harmony patch targets still exist in the game | - | PatchTargetsTests | - | - | Automated |
| EXAMPLE value: clamp + owner gate | ExampleRulesTests | ExampleCommandFlowTests | example-command | - | Automated (replace) |
| Persistence (relog) | | | | | Gap |
| Multiplayer (virtual player `peerJoin`) | | | | real 2-player smoke test | Gap |
| KY-Items contributions | | | | | Gap |
