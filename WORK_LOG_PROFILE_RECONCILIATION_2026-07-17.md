# Daily Avatar Reset And Profile Deduplication

Date: 2026-07-17 JST

## Findings

- The production API returned profiles saved on July 15 and 16 together with
  current JST July 17 profiles. A fresh Unity launch could therefore recreate
  previous-day avatars.
- Unity used `user_id + display_name + last_seen_at` as its processed key.
  Re-saving the same account changed `last_seen_at`, so it was treated as a new
  avatar instead of an update.
- At the time of inspection, three profiles in the production response had
  been saved on July 17 JST. Those were real current-day registrations, not
  stale Unity objects.

## Changes

- `/profiles/recent` now returns at most ten profiles whose `profile_saved_at`
  is both within the current JST date and within the last ten minutes.
- `CrowdAvatarManager` reconciles the latest successful API snapshot by stable
  `user_id`. A repeated save updates the existing avatar's coordinate, name,
  messages, and ten-minute timer.
- User avatars absent from the next successful snapshot are destroyed. This
  removes expired users and previous-day users on the first poll after JST
  midnight while keeping CPU fallback behavior independent.
- `AvatarView.Initialize` restores full opacity when an existing avatar's
  display timer is restarted.

## Verification

- Cloudflare validation: 9 test files, 30 tests passed; typecheck and lint passed.
- Unity 6000.3.11f1 batch compilation passed.
- The only Unity compiler warning is the existing obsolete
  `Object.FindObjectsOfType` call in `AvatarCounter.cs`.
