// indexedDB open
export async function open(name, version) {
  let request = new Promise((resolve) => {
    console.debug(`Opening IndexedDB ${name} version ${version}`);
    const openRequest = indexedDB.open(name, version);

    let jsObjectReference =  DotNet.createJSObjectReference(openRequest)
    dotnetHelper.invokeMethodAsync('OnOpen', jsObjectReference);
    dotnetHelper.disposeJSObjectReference(jsObjectReference);

    openRequest.onupgradeneeded = (event) => {
      const db = openRequest.result;
      db.createObjectStore('store', {keyPath: 'id'});
    }
    
    openRequest.onsuccess = () => {
      console.info(`Opened IndexedDB ${name} version ${version}`);
      DotNet.invokeMethodAsync('IdxDb', 'OnUpgradeNeeded', 'Hello Darkness My Old Friend');
      resolve(openRequest.result);
    };
    
    openRequest.onerror = () => {
      console.error(`Error opening IndexedDB ${name} version ${version}`);
      resolve(null);
    };
  })
  
  return await request;
}

// List all databases
export async function databases() {
  console.debug('List all databases');
  const databaseInfos = await indexedDB.databases();
  console.info(databaseInfos);
  return databaseInfos;
}

// The cmp() method of the IDBFactory interface compares two values as keys to determine equality and ordering for IndexedDB operations, such as storing and iterating.
export async function cmp(a, b) {
  console.debug(`Comparing ${a} and ${b}`);
  let result = indexedDB.cmp(a, b);
  console.info(`Comparison results: ${result}`);
  return result;
}

// The deleteDatabase() method of the IDBFactory interface requests the deletion of a database.
export async function deleteDatabase(name) {
  let request = new Promise((resolve) => {
    console.debug(`Deleting IndexedDB ${name}`);
    const deleteRequest = indexedDB.deleteDatabase(name);

    deleteRequest.onerror = () => {
      console.error(`Error deleting IndexedDB ${name}`);
      resolve(null);
    };

    deleteRequest.onsuccess = () => {
      console.info(`Deleted IndexedDB ${name}`);
      resolve(deleteRequest.result);
    };
  })

  return await request;
}