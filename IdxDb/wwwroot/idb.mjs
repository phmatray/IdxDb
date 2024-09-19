/**
 * A cache to store opened database instances to prevent reopening.
 * @type {Map<string, IDBDatabase>}
 */
const dbCache = new Map();

/**
 * Opens an IndexedDB database and caches the connection.
 * @param {string} dbName - The name of the database.
 * @param {number} [version=1] - The version number of the database.
 * @param {function} [upgradeCallback=null] - Optional callback for handling database upgrades.
 * @returns {Promise<IDBDatabase>} - A promise that resolves to the database instance.
 */
export async function openIndexedDB(dbName, version = 1, upgradeCallback = null) {
  if (dbCache.has(dbName)) {
    return dbCache.get(dbName);
  }

  return new Promise((resolve, reject) => {
    const request = indexedDB.open(dbName, version);

    request.onupgradeneeded = (event) => {
      const db = event.target.result;
      if (upgradeCallback) {
        upgradeCallback(db, event);
      }
    };

    request.onsuccess = (event) => {
      const db = event.target.result;
      dbCache.set(dbName, db);
      resolve(db);
    };

    request.onerror = (event) => {
      reject(event.target.error);
    };
  });
}

/**
 * Upgrades the database schema, adding or modifying object stores and indexes.
 * @param {string} dbName - The name of the database.
 * @param {number} newVersion - The new version number for the database.
 * @param {Array<object>} storeSchemas - An array of store schema definitions.
 * @returns {Promise<void>}
 */
export async function upgradeDatabase(dbName, newVersion, storeSchemas) {
  await openIndexedDB(dbName, newVersion, (db, event) => {
    storeSchemas.forEach((schema) => {
      if (!db.objectStoreNames.contains(schema.name)) {
        const store = db.createObjectStore(schema.name, schema.options);
        if (schema.indexes) {
          schema.indexes.forEach((index) => {
            store.createIndex(index.name, index.keyPath, { unique: index.unique });
          });
        }
      } else if (schema.modify) {
        // Handle modifications like adding indexes
        const store = event.target.transaction.objectStore(schema.name);
        schema.indexes.forEach((index) => {
          if (!store.indexNames.contains(index.name)) {
            store.createIndex(index.name, index.keyPath, { unique: index.unique });
          }
        });
      }
    });
  });
}

/**
 * Creates an index on an object store.
 * @param {string} dbName - The name of the database.
 * @param {string} storeName - The name of the object store.
 * @param {string} indexName - The name of the index to create.
 * @param {string|string[]} keyPath - The key path(s) for the index.
 * @param {boolean} [unique=false] - Whether the index should enforce unique values.
 * @returns {Promise<void>}
 */
export async function createIndex(dbName, storeName, indexName, keyPath, unique = false) {
  await upgradeDatabase(dbName, undefined, [
    {
      name: storeName,
      modify: true,
      indexes: [{ name: indexName, keyPath: keyPath, unique: unique }],
    },
  ]);
}

/**
 * Clears all records from an object store.
 * @param {string} dbName - The name of the database.
 * @param {string} storeName - The name of the object store.
 * @returns {Promise<boolean>} - A promise that resolves to true if the operation is successful.
 */
export async function clearStore(dbName, storeName) {
  try {
    const db = await openIndexedDB(dbName);
    return new Promise((resolve, reject) => {
      const transaction = db.transaction(storeName, 'readwrite');
      const store = transaction.objectStore(storeName);
      const request = store.clear();

      request.onsuccess = () => resolve(true);
      request.onerror = (event) => reject(event.target.error);
    });
  } catch (error) {
    throw error;
  }
}

/**
 * Retrieves all items from an object store.
 * @param {string} dbName - The name of the database.
 * @param {string} storeName - The name of the object store.
 * @returns {Promise<Array>} - A promise that resolves to an array of items.
 */
export async function getAll(dbName, storeName) {
  try {
    const db = await openIndexedDB(dbName);
    return new Promise((resolve, reject) => {
      const transaction = db.transaction(storeName, 'readonly');
      const store = transaction.objectStore(storeName);
      const request = store.getAll();

      request.onsuccess = (event) => resolve(event.target.result);
      request.onerror = (event) => reject(event.target.error);
    });
  } catch (error) {
    throw error;
  }
}

/**
 * Retrieves all items that match a query on a specific index.
 * @param {string} dbName - The name of the database.
 * @param {string} storeName - The name of the object store.
 * @param {string} indexName - The name of the index to query.
 * @param {*} query - The query value or IDBKeyRange.
 * @returns {Promise<Array>} - A promise that resolves to an array of matching items.
 */
export async function getAllByIndex(dbName, storeName, indexName, query) {
  try {
    const db = await openIndexedDB(dbName);
    return new Promise((resolve, reject) => {
      const transaction = db.transaction(storeName, 'readonly');
      const store = transaction.objectStore(storeName);
      const index = store.index(indexName);
      const request = index.getAll(query);

      request.onsuccess = (event) => resolve(event.target.result);
      request.onerror = (event) => reject(event.target.error);
    });
  } catch (error) {
    throw error;
  }
}

/**
 * Retrieves a single item by its key from an object store.
 * @param {string} dbName - The name of the database.
 * @param {string} storeName - The name of the object store.
 * @param {*} id - The key of the item to retrieve.
 * @returns {Promise<object>} - A promise that resolves to the item, or undefined if not found.
 */
export async function getOne(dbName, storeName, id) {
  try {
    const db = await openIndexedDB(dbName);
    return new Promise((resolve, reject) => {
      const transaction = db.transaction(storeName, 'readonly');
      const store = transaction.objectStore(storeName);
      const request = store.get(id);

      request.onsuccess = (event) => resolve(event.target.result);
      request.onerror = (event) => reject(event.target.error);
    });
  } catch (error) {
    throw error;
  }
}

/**
 * Adds a single item to an object store.
 * @param {string} dbName - The name of the database.
 * @param {string} storeName - The name of the object store.
 * @param {object} item - The item to add.
 * @returns {Promise<boolean>} - A promise that resolves to true if the operation is successful.
 */
export async function addOne(dbName, storeName, item) {
  try {
    const db = await openIndexedDB(dbName);
    return new Promise((resolve, reject) => {
      const transaction = db.transaction(storeName, 'readwrite');
      const store = transaction.objectStore(storeName);
      const request = store.add(item);

      request.onsuccess = () => resolve(true);
      request.onerror = (event) => reject(event.target.error);
    });
  } catch (error) {
    throw error;
  }
}

/**
 * Adds multiple items to an object store in a single transaction.
 * @param {string} dbName - The name of the database.
 * @param {string} storeName - The name of the object store.
 * @param {Array<object>} items - An array of items to add.
 * @returns {Promise<boolean>} - A promise that resolves to true if the operation is successful.
 */
export async function addMany(dbName, storeName, items) {
  try {
    const db = await openIndexedDB(dbName);
    return new Promise((resolve, reject) => {
      const transaction = db.transaction(storeName, 'readwrite');
      const store = transaction.objectStore(storeName);

      items.forEach((item) => {
        store.add(item);
      });

      transaction.oncomplete = () => resolve(true);
      transaction.onerror = (event) => reject(event.target.error);
    });
  } catch (error) {
    throw error;
  }
}

/**
 * Updates an existing item in an object store.
 * @param {string} dbName - The name of the database.
 * @param {string} storeName - The name of the object store.
 * @param {object} item - The item to update.
 * @returns {Promise<boolean>} - A promise that resolves to true if the operation is successful.
 */
export async function updateOne(dbName, storeName, item) {
  try {
    const db = await openIndexedDB(dbName);
    return new Promise((resolve, reject) => {
      const transaction = db.transaction(storeName, 'readwrite');
      const store = transaction.objectStore(storeName);
      const request = store.put(item);

      request.onsuccess = () => resolve(true);
      request.onerror = (event) => reject(event.target.error);
    });
  } catch (error) {
    throw error;
  }
}

/**
 * Deletes an item from an object store by its key.
 * @param {string} dbName - The name of the database.
 * @param {string} storeName - The name of the object store.
 * @param {*} id - The key of the item to delete.
 * @returns {Promise<boolean>} - A promise that resolves to true if the operation is successful.
 */
export async function deleteOne(dbName, storeName, id) {
  try {
    const db = await openIndexedDB(dbName);
    return new Promise((resolve, reject) => {
      const transaction = db.transaction(storeName, 'readwrite');
      const store = transaction.objectStore(storeName);
      const request = store.delete(id);

      request.onsuccess = () => resolve(true);
      request.onerror = (event) => reject(event.target.error);
    });
  } catch (error) {
    throw error;
  }
}

/**
 * Counts the number of records in an object store.
 * @param {string} dbName - The name of the database.
 * @param {string} storeName - The name of the object store.
 * @returns {Promise<number>} - A promise that resolves to the count of records.
 */
export async function count(dbName, storeName) {
  try {
    const db = await openIndexedDB(dbName);
    return new Promise((resolve, reject) => {
      const transaction = db.transaction(storeName, 'readonly');
      const store = transaction.objectStore(storeName);
      const request = store.count();

      request.onsuccess = (event) => resolve(event.target.result);
      request.onerror = (event) => reject(event.target.error);
    });
  } catch (error) {
    throw error;
  }
}

/**
 * The current transaction instance.
 */
let currentTransaction = null;

/**
 * Begins a transaction across one or more object stores.
 * @param {string} dbName - The name of the database.
 * @param {string|string[]} storeNames - The name(s) of the object store(s).
 * @param {string} mode - The transaction mode ('readonly' or 'readwrite').
 * @returns {Promise<void>}
 */
export async function beginTransaction(dbName, storeNames, mode) {
  const db = await openIndexedDB(dbName);
  currentTransaction = db.transaction(storeNames, mode);
}

/**
 * Commits the current transaction.
 * @returns {Promise<void>}
 */
export async function commitTransaction() {
  if (currentTransaction) {
    currentTransaction.commit();
    currentTransaction = null;
  }
}
