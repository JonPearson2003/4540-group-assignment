# ECU Lost and Found


> A university lost and found an application for students to find missing items they may have lost on campus.


## About
ECU lost and Found lets faculty and Students upload and browse lost items that have been found on campus and where those items were returned too.


## Prerequisites to run code
- .NET SDK version of "11.0.100-preview.7.26381.103" or newer
- .NET MAUI workload
- A working emulator


## Week 1 Progress
- Set up up Maui Project
- Set up the SQlite
- Set up the Mock API's
- Picked a Domain entity
- Template for the application was made


## Week 2 Going Forward
- Now that template is made work on adding specific components to the code
- Discuss in more detail how we want the app to look and its functions
## Week 2 Progress
- Implemented CRUD operations
- Implemented change tracking

### Before Change Screenshots
#### UI Before Change
<img width="440" height="871" alt="Screenshot (51)" src="https://github.com/user-attachments/assets/2881b02c-4489-4cc3-85c1-ae315740ae02" />

#### Items Database Before Change
<img width="1920" height="1080" alt="Screenshot (52)" src="https://github.com/user-attachments/assets/3ad651bb-d849-4b3c-8569-048961e868bb" />

#### Change Tracking Before Change
<img width="1920" height="1080" alt="Screenshot (53)" src="https://github.com/user-attachments/assets/e81a7caf-df26-427f-89da-83a457e94c39" />

### After Change Screenshots
#### UI After Change
<img width="441" height="858" alt="Screenshot (54)" src="https://github.com/user-attachments/assets/a4c83279-091f-49a2-86bd-456f4ff196c9" />

#### Items Database After Change
<img width="1920" height="1080" alt="Screenshot (55)" src="https://github.com/user-attachments/assets/58dcb6e9-8ba6-4c4b-9eba-20bb9597c914" />

#### Change Tracking After Changes
<img width="1920" height="1080" alt="Screenshot (56)" src="https://github.com/user-attachments/assets/e486c554-b308-4376-905d-73f695aab0e7" />


## Week 3 Going Forward
- Implement a push endpoint to server
- Implement  pending local changes to sync to server
- Implement pending local changes after pushing to server

## Before Pending Changes Screenshot
![App before pending changes were added](./Images/screenshots/before-pending-changes.png)


## After Pending Changes Implementation Screenshot
![App showing a queued create operation under Pending sync changes](./Images/screenshots/after-pending-changes.png)

## Week 4 Going Forward
- Add request server changes newer than the device's last sync time
- Apply incoming chnages to the local database
- Be able to handle deletes (tombstones or flags)

## Week 4 Progress
- Added Conflict Handeling
- Added Sync Trigger and Automatic Sync when connected to internet
- Added Retry Sync Logic
- Added Audit Trail

## Evidence of conflict strategy working
### Steps taken
- App is ran on two seperate devices(Device A and Device B)
- Device B edits and item on the list while offline
- Device A edits the same item on the list while offline
- Device A reconnects to the internet and syncs
- Device B reconnects to the internet and syncs
- Device A resyncs and shows the changes Device B made

- App uses last Process wins rule
<p>
<img src="./Images/screenshots/week%205/1_conflict_strat_app_only.png" alt="Automatically Syncs when reconnected to the internet" width="50%">
</p>
- Sever tracks server version and app only pulls new server versions it doesnt have

<img src="./Images/screenshots/week%205/1_conflict_strat_sever_version.png" alt="Automatically Syncs when reconnected to the internet">

## Evidence of audit logging on device
<p>
<img src="./Images/screenshots/week%205/2_audit_on_app.png" alt="Automatically Syncs when reconnected to the internet" width="50%">
</p>

## Evidence of audit logging synced to server
<img src="./Images/screenshots/week%205/3_audit_on_server.png" alt="Automatically Syncs when reconnected to the internet">

## Evidence of automatic sync attempt both (online and offline)
- Attempting to sync while in airplane mode
<p>
<img src="./Images/screenshots/week%205/4_offline_sync.png" alt="Automatically Syncs when reconnected to the internet" width="50%">
</p>
- Automatically Syncs when reconnected to the internet
<p>
<img src="./Images/screenshots/week%205/4_online_auto_sync.png" alt="Automatically Syncs when reconnected to the internet" width="50%">
</p>

### Video Demo of automatic sync


https://github.com/user-attachments/assets/31a4b6ef-e9a5-40f4-860f-dc8408756a56


