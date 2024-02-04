/**
 * @license
 * Copyright 2023 Google LLC
 * SPDX-License-Identifier: Apache-2.0
 */

import $ from 'jquery';

import * as Blockly from 'blockly/core';

const storageKey = 'mainWorkspace';

const serialize = function (workspace) {
	const data = Blockly.serialization.workspaces.save(workspace);
	return JSON.stringify(data);
};

const deserialize = function (workspace, data) {
	// Don't emit events during loading.
	Blockly.Events.disable();
	Blockly.serialization.workspaces.load(JSON.parse(data), workspace, false);
	Blockly.Events.enable();
};

/**
 * Saves the state of the workspace to browser's local storage.
 * @param {Blockly.Workspace} workspace Blockly workspace to save.
 */
export const saveWorkspace = function (workspace) {
	window.localStorage?.setItem(storageKey, serialize(workspace));
};

/**
 * Loads saved state from local storage into the given workspace.
 * @param {Blockly.Workspace} workspace Blockly workspace to load into.
 */
export const loadWorkspace = function (workspace) {
	const data = window.localStorage?.getItem(storageKey);
	if (!data) return;
	deserialize(workspace, data);
};

export const saveToFile = function (workspace) {
	const element = $('<a>').attr({
		href: 'data:text/plain;charset=utf-8,' + encodeURIComponent(serialize(workspace)),
		download: 'workspace.json',
	}).css('display', 'none');
	element.appendTo($('body'));
	element[0].click();
	element.remove();
};

let currentWorkspace;
const loadFromFileInput = $('<input>')
	.attr({
		type: 'file',
		accept: '.json',
	})
	.css('display', 'none')
	.on('change', e => {
		loadFromFile(currentWorkspace, e.target.files[0]);
	});
loadFromFileInput.appendTo($('body'));

export const loadFromFile = function (workspace, file) {
	if (file) {
		const reader = new FileReader();
		reader.addEventListener('load', e => {
			deserialize(workspace, e.target.result);
		});
		reader.readAsText(file);
	}
	else {
		currentWorkspace = workspace;
		loadFromFileInput.val(null);
		loadFromFileInput.click();
	}
};
