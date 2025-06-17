/**
 * IndexedDB operations optimized for LINQ-to-IndexedDB approach
 * This module provides a minimal API surface for database operations
 */

/**
 * Cache for open database connections
 * @type {Map<string, IDBDatabase>}
 */
const dbCache = new Map();

/**
 * Opens or retrieves a cached IndexedDB database connection
 * @param {string} dbName - Database name
 * @param {number} version - Database version
 * @param {Array} stores - Store definitions
 * @returns {Promise<IDBDatabase>}
 */
export async function openDatabase(dbName, version, stores) {
    const cacheKey = `${dbName}_v${version}`;
    
    if (dbCache.has(cacheKey)) {
        const cached = dbCache.get(cacheKey);
        if (cached.version === version) {
            console.log(`Using cached database connection for ${dbName} v${version}`);
            return cached;
        }
        cached.close();
        dbCache.delete(cacheKey);
    }

    console.log(`Opening database ${dbName} v${version} with stores:`, stores.map(s => s.name));

    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, version);

        request.onupgradeneeded = (event) => {
            const db = event.target.result;
            const transaction = event.target.transaction;
            const oldVersion = event.oldVersion;
            
            console.log(`Upgrading database from version ${oldVersion} to ${version}`);

            // Create object stores based on definitions
            stores.forEach(store => {
                if (!db.objectStoreNames.contains(store.name)) {
                    console.log(`Creating object store: ${store.name}`);
                    const objectStore = db.createObjectStore(store.name, {
                        keyPath: store.options.keyPath,
                        autoIncrement: store.options.autoIncrement
                    });

                    // Create indexes
                    if (store.indexes && store.indexes.length > 0) {
                        store.indexes.forEach(index => {
                            console.log(`Creating index ${index.name} on ${store.name}`);
                            objectStore.createIndex(index.name, index.keyPath, {
                                unique: index.unique || false,
                                multiEntry: index.multiEntry || false
                            });
                        });
                    }
                } else {
                    console.log(`Object store ${store.name} already exists`);
                }
            });
        };

        request.onsuccess = (event) => {
            const db = event.target.result;
            console.log(`Database ${dbName} opened successfully. Version: ${db.version}, Stores:`, Array.from(db.objectStoreNames));
            
            // Verify all required stores exist
            const missingStores = stores.filter(s => !db.objectStoreNames.contains(s.name));
            if (missingStores.length > 0) {
                console.error(`Database is missing required stores: ${missingStores.map(s => s.name).join(', ')}`);
                console.error(`This usually happens when the database was created with a different schema.`);
                console.error(`To fix this, either:`);
                console.error(`1. Increase the version number in your code`);
                console.error(`2. Delete the database from DevTools > Application > IndexedDB`);
                db.close();
                reject(new Error(`Database is missing required stores: ${missingStores.map(s => s.name).join(', ')}. Please clear your browser's IndexedDB for this site or increase the database version.`));
                return;
            }
            
            dbCache.set(cacheKey, db);
            resolve(db);
        };

        request.onerror = () => {
            reject(new Error(`Failed to open database: ${request.error}`));
        };
    });
}

/**
 * Executes a query on an object store
 * @param {string} dbName - Database name
 * @param {string} storeName - Store name
 * @param {Object} queryOptions - Query options (filters, sorting, pagination)
 * @returns {Promise<Array>}
 */
export async function executeQuery(dbName, storeName, queryOptions = {}) {
    const db = await getDatabase(dbName);
    const transaction = db.transaction([storeName], 'readonly');
    const store = transaction.objectStore(storeName);
    
    return new Promise((resolve, reject) => {
        const results = [];
        let request;
        
        // Determine the source (store or index)
        let source = store;
        if (queryOptions.indexName) {
            source = store.index(queryOptions.indexName);
        }
        
        // Create cursor with optional range
        if (queryOptions.range) {
            request = source.openCursor(queryOptions.range, queryOptions.direction || 'next');
        } else {
            request = source.openCursor(null, queryOptions.direction || 'next');
        }
        
        let skipped = 0;
        let taken = 0;
        
        request.onsuccess = (event) => {
            const cursor = event.target.result;
            
            if (!cursor) {
                resolve(results);
                return;
            }
            
            // Skip logic
            if (queryOptions.skip && skipped < queryOptions.skip) {
                skipped++;
                cursor.continue();
                return;
            }
            
            // Take logic
            if (queryOptions.take && taken >= queryOptions.take) {
                resolve(results);
                return;
            }
            
            // Filter logic (client-side filtering for complex conditions)
            if (queryOptions.filter) {
                const record = cursor.value;
                if (evaluateFilter(record, queryOptions.filter)) {
                    results.push(record);
                    taken++;
                }
            } else {
                results.push(cursor.value);
                taken++;
            }
            
            cursor.continue();
        };
        
        request.onerror = () => {
            reject(new Error(`Query failed: ${request.error}`));
        };
    });
}

/**
 * Gets a single record by key
 * @param {string} dbName - Database name
 * @param {string} storeName - Store name
 * @param {*} key - Record key
 * @returns {Promise<Object|null>}
 */
export async function getByKey(dbName, storeName, key) {
    const db = await getDatabase(dbName);
    const transaction = db.transaction([storeName], 'readonly');
    const store = transaction.objectStore(storeName);
    
    return new Promise((resolve, reject) => {
        const request = store.get(key);
        
        request.onsuccess = () => {
            resolve(request.result || null);
        };
        
        request.onerror = () => {
            reject(new Error(`Get failed: ${request.error}`));
        };
    });
}

/**
 * Adds a record to the store
 * @param {string} dbName - Database name
 * @param {string} storeName - Store name
 * @param {Object} record - Record to add
 * @returns {Promise<*>} The key of the added record
 */
export async function add(dbName, storeName, record) {
    const db = await getDatabase(dbName);
    const transaction = db.transaction([storeName], 'readwrite');
    const store = transaction.objectStore(storeName);
    
    return new Promise((resolve, reject) => {
        const request = store.add(record);
        
        request.onsuccess = () => {
            resolve(request.result);
        };
        
        request.onerror = () => {
            reject(new Error(`Add failed: ${request.error}`));
        };
    });
}

/**
 * Updates a record in the store
 * @param {string} dbName - Database name
 * @param {string} storeName - Store name
 * @param {Object} record - Record to update
 * @returns {Promise<*>} The key of the updated record
 */
export async function update(dbName, storeName, record) {
    const db = await getDatabase(dbName);
    const transaction = db.transaction([storeName], 'readwrite');
    const store = transaction.objectStore(storeName);
    
    return new Promise((resolve, reject) => {
        const request = store.put(record);
        
        request.onsuccess = () => {
            resolve(request.result);
        };
        
        request.onerror = () => {
            reject(new Error(`Update failed: ${request.error}`));
        };
    });
}

/**
 * Deletes a record by key
 * @param {string} dbName - Database name
 * @param {string} storeName - Store name
 * @param {*} key - Record key
 * @returns {Promise<void>}
 */
export async function deleteByKey(dbName, storeName, key) {
    const db = await getDatabase(dbName);
    const transaction = db.transaction([storeName], 'readwrite');
    const store = transaction.objectStore(storeName);
    
    return new Promise((resolve, reject) => {
        const request = store.delete(key);
        
        request.onsuccess = () => {
            resolve();
        };
        
        request.onerror = () => {
            reject(new Error(`Delete failed: ${request.error}`));
        };
    });
}

/**
 * Counts records in a store with optional filter
 * @param {string} dbName - Database name
 * @param {string} storeName - Store name
 * @param {Object} queryOptions - Query options
 * @returns {Promise<number>}
 */
export async function count(dbName, storeName, queryOptions = {}) {
    const db = await getDatabase(dbName);
    console.log(`Counting records in ${dbName}.${storeName}, available stores:`, Array.from(db.objectStoreNames));
    
    if (!db.objectStoreNames.contains(storeName)) {
        throw new Error(`Store '${storeName}' not found in database '${dbName}'. Available stores: ${Array.from(db.objectStoreNames).join(', ')}`);
    }
    
    const transaction = db.transaction([storeName], 'readonly');
    const store = transaction.objectStore(storeName);
    
    // If no filter, use native count
    if (!queryOptions.filter && !queryOptions.range) {
        return new Promise((resolve, reject) => {
            const request = store.count();
            
            request.onsuccess = () => {
                resolve(request.result);
            };
            
            request.onerror = () => {
                reject(new Error(`Count failed: ${request.error}`));
            };
        });
    }
    
    // Otherwise, count using cursor
    const results = await executeQuery(dbName, storeName, {
        ...queryOptions,
        skip: 0,
        take: undefined
    });
    
    return results.length;
}

/**
 * Clears all records from a store
 * @param {string} dbName - Database name
 * @param {string} storeName - Store name
 * @returns {Promise<void>}
 */
export async function clear(dbName, storeName) {
    const db = await getDatabase(dbName);
    const transaction = db.transaction([storeName], 'readwrite');
    const store = transaction.objectStore(storeName);
    
    return new Promise((resolve, reject) => {
        const request = store.clear();
        
        request.onsuccess = () => {
            resolve();
        };
        
        request.onerror = () => {
            reject(new Error(`Clear failed: ${request.error}`));
        };
    });
}

/**
 * Closes a database connection and removes it from cache
 * @param {string} dbName - Database name
 */
export function closeDatabase(dbName) {
    for (const [key, db] of dbCache.entries()) {
        if (key.startsWith(dbName)) {
            db.close();
            dbCache.delete(key);
        }
    }
}

/**
 * Deletes the entire database
 * @param {string} dbName - Database name
 * @returns {Promise<void>}
 */
export async function deleteDatabase(dbName) {
    // Close any open connections first
    closeDatabase(dbName);
    
    return new Promise((resolve, reject) => {
        const request = indexedDB.deleteDatabase(dbName);
        
        request.onsuccess = () => {
            console.log(`Database ${dbName} deleted successfully`);
            resolve();
        };
        
        request.onerror = () => {
            reject(new Error(`Failed to delete database: ${request.error}`));
        };
    });
}

/**
 * Gets a database from cache or throws error
 * @param {string} dbName - Database name
 * @returns {IDBDatabase}
 */
function getDatabase(dbName) {
    for (const [key, db] of dbCache.entries()) {
        if (key.startsWith(dbName)) {
            return db;
        }
    }
    throw new Error(`Database ${dbName} is not open. Call openDatabase first.`);
}

/**
 * Evaluates a filter object against a record
 * @param {Object} record - The record to evaluate
 * @param {Object} filter - The filter conditions
 * @returns {boolean}
 */
function evaluateFilter(record, filter) {
    // Simple filter evaluation - can be extended for complex queries
    for (const [key, condition] of Object.entries(filter)) {
        const value = record[key];
        
        if (typeof condition === 'object' && condition !== null) {
            // Handle operators
            if ('$eq' in condition && value !== condition.$eq) return false;
            if ('$ne' in condition && value === condition.$ne) return false;
            if ('$gt' in condition && !(value > condition.$gt)) return false;
            if ('$gte' in condition && !(value >= condition.$gte)) return false;
            if ('$lt' in condition && !(value < condition.$lt)) return false;
            if ('$lte' in condition && !(value <= condition.$lte)) return false;
            if ('$in' in condition && !condition.$in.includes(value)) return false;
            if ('$contains' in condition && !value.includes(condition.$contains)) return false;
        } else {
            // Direct equality
            if (value !== condition) return false;
        }
    }
    
    return true;
}